(import "Image.cake"
 &comptime-only "CHelpers.cake")
(c-import "stdio.h" ;; fprintf, strncmp
          "math.h") ;; sqrtf

;;
;; Background/wallpaper determination
;;

(comptime-cond
 ('Unix
  ;; Gnome GTK
  (add-c-search-directory-module "/usr/include/glib-2.0")
  (add-c-search-directory-module "/usr/include/glib-2.0")
  (add-c-search-directory-module "/usr/lib/x86_64-linux-gnu/glib-2.0/include")
  (c-import "gio/gio.h")
  (add-library-dependency "glib-2.0" "gio-2.0" "gobject-2.0")

  (defun-local auto-color-get-current-background-filename (error-string (* (* (const char)))
                                                           &return (* (const char)))
    (var g-settings (* GSettings) (g_settings_new "org.gnome.desktop.background"))
    (unless g-settings
      (set (deref error-string) "Unable to get GTK settings for org.gnome.desktop.background")
      (return null))
    (var background (* gchar) (g_settings_get_string g-settings "picture-uri"))
    (unless background
      (set (deref error-string) "Unable to get picture-uri from org.gnome.desktop.background")
      (g_object_unref g-settings)
      (set g-settings null)
      (return null))

    (g_object_unref g-settings)
    (set g-settings null)

    (var file-uri (* (const char)) "file://")
    (var file-uri-prefix-length size_t (strlen file-uri))
    (unless (= 0 (strncmp file-uri background file-uri-prefix-length))
      (set (deref error-string) "Unable to process picture-uri: uri type not supported")
      (return null))

    (set background (strdup (+ background file-uri-prefix-length)))
    (return background))))

;;
;; Image data
;;

(defstruct-local auto-color-image
  width int
  height int
  pixel-data (* (unsigned char)))

(defun-local auto-color-image-destroy (image-data (* auto-color-image))
  (stbi_image_free (path image-data > pixel-data)))

(defun-local auto-color-load-image (image-to-load (* (const char))
                                    image-data-out (* auto-color-image) &return bool)
  (var num-pixel-components int 0)
  (var num-desired-channels int 3)
  (var pixel-data (* (unsigned char))
    (stbi_load image-to-load
               (addr (path image-data-out > width)) (addr (path image-data-out > height))
               (addr num-pixel-components) num-desired-channels))
  (unless pixel-data
    (fprintf stderr "error: failed to load %s with message: %s\n" image-to-load
             (stbi_failure_reason))
    (return false))
  (set (path image-data-out > pixel-data) pixel-data)
  (return true))

;;
;; Color selection
;;
(def-type-alias auto-color-color ([] 3 (unsigned char)))

(defun-local auto-color-pick-colors-by-threshold (image-data (* auto-color-image)
                                                  colors-out (* auto-color-color)
                                                  num-colors-requested (unsigned char))
  (var color-samples ([] 512 auto-color-color))

  ;; Dynamically adjust sampling based on resolution to keep a somewhat constant number of samples
  (var sample-skip-x int 0)
  (var sample-skip-y int 0)
  (scope
   (var samples-per-x int 1)
   (var samples-per-y int 1)
   (var samples-per-axis float (sqrtf (array-size color-samples)))
   (set samples-per-x
        (* samples-per-axis ;; Evenly distribute samples based on aspect ratio
           (/ (path image-data > width)
              (type-cast (path image-data > height) float))))
   (set samples-per-y
        (* samples-per-axis ;; Evenly distribute samples based on aspect ratio
           (/ (type-cast (path image-data > height) float)
              (path image-data > width))))
   (var num-color-samples int (* samples-per-x samples-per-y))
   (set sample-skip-x (/ (path image-data > width)
                         (- samples-per-x 1))) ;; -1 to account for 0 index
   (set sample-skip-y (/ (path image-data > height)
                         (- samples-per-y 1)))
   (fprintf stderr "Samples: %dx%d for total of %d samples. Sample every %dx%d pixel
 of the %dx%d image\n"
            samples-per-x samples-per-y num-color-samples
            sample-skip-x sample-skip-y
            (path image-data > width) (path image-data > height)))

  (var current-sample-write (* auto-color-color) color-samples)
  (c-for (var y int 0) (< y (path image-data > height)) (set y (+ y sample-skip-y))
    (c-for (var x int 0) (< x (path image-data > width)) (set x (+ x sample-skip-x))
      (var pixel-index int (* 3 (+ (* y (path image-data > width)) x)))
      (var pixel-color-component (* (unsigned char))
        (addr (at pixel-index (path image-data > pixel-data))))
      (each-in-range 3 i
        (set (at i (deref current-sample-write)) (at i pixel-color-component)))
      (incr current-sample-write)))

  ;; This isn't exactly equal to the color-samples array size due to the even sample distribution
  (var num-samples int (- current-sample-write color-samples))
  (fprintf stderr "Sampled %d pixels" num-samples)

  (var num-distinct-colors int 0)
  ;; Do color threshold selection
  (each-in-range num-samples sample-index
    (var is-distinct bool true)
    (each-in-range num-distinct-colors distinct-color-index
      (set is-distinct false)
      (break))
    (unless is-distinct ;; Already represented
      (continue))

    (incr num-distinct-colors)
    ;; TODO: Consider what to do to not bias color selection to the first samples. Shuffle pixels?
    ;; Evict distinct colors randomly? Pick more colors than requested, then randomly narrow?
    (when (>= num-distinct-colors num-colors-requested)
      (break))))

;;
;; Interface
;;

(defun auto-color-pick-from-current-background (&return bool)
  (var error-string (* (const char)) null)
  (var background-filename (* (const char))
    (auto-color-get-current-background-filename (addr error-string)))
  (unless background-filename
    (fprintf stderr "error: %s" error-string)
    (return false))
  (fprintf stderr "\nPicking colors from '%s'\n" background-filename)

  (var image-data auto-color-image (array 0))
  (unless (auto-color-load-image background-filename (addr image-data))
    (free (type-cast background-filename (* void)))
    (return false))

  (var colors ([] 16 auto-color-color))
  (var num-colors-requested (unsigned char) (array-size colors))
  (auto-color-pick-colors-by-threshold (addr image-data) colors num-colors-requested)

  (auto-color-image-destroy (addr image-data))
  (free (type-cast background-filename (* void)))
  (return true))
