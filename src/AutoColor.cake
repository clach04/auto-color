(import "Image.cake"
 &comptime-only "CHelpers.cake")
(c-import "stdio.h") ;; fprintf

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

  (auto-color-image-destroy (addr image-data))
  (free (type-cast background-filename (* void)))
  (return true))
