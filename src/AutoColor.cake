(import "Image.cake"
        &comptime-only "CHelpers.cake")
(c-import "<stdio.h>" ;; fprintf, strncmp
          "<stdlib.h>" ;; qsort
          "<string.h>" ;; memcmp
          "<math.h>") ;; sqrtf

(var-global g-auto-color-should-print bool false)
(defmacro debug-print (format string &optional &rest arguments any)
  (if arguments
    (tokenize-push output
      (when g-auto-color-should-print
        (fprintf stderr (token-splice format) (token-splice-rest arguments tokens))))
    (tokenize-push output
      (when g-auto-color-should-print
        (fprintf stderr (token-splice format)))))
  (return true))

;;
;; Background/wallpaper determination
;;

(comptime-cond
 ('Unix
  ;; Gnome GTK
  (add-c-search-directory-module "/usr/include/glib-2.0")
  (add-c-search-directory-module "/usr/lib/x86_64-linux-gnu/glib-2.0/include")
  (c-import "gio/gio.h")
  (add-library-dependency "glib-2.0" "gio-2.0" "gobject-2.0")

  (add-c-build-dependency "UriUnescape.c")

  (declare-extern-function uriUnescapeInPlaceEx (in-out (* char) &return (* (const char))))

  (defun-local auto-color-get-current-background-filename (wallpaper-out (* char)
                                                           wallpaper-out-size (unsigned int)
                                                           error-string (* (* (const char)))
                                                           &return bool)
    (var g-settings (* GSettings) (g_settings_new "org.gnome.desktop.background"))
    (unless g-settings
      (set (deref error-string) "Unable to get GTK settings for org.gnome.desktop.background")
      (return false))
    (var background (* gchar) (g_settings_get_string g-settings "picture-uri"))
    (unless background
      (set (deref error-string) "Unable to get picture-uri from org.gnome.desktop.background")
      (g_object_unref g-settings)
      (set g-settings null)
      (return false))

    (g_object_unref g-settings)
    (set g-settings null)

    (var file-uri (* (const char)) "file://")
    (var file-uri-prefix-length size_t (strlen file-uri))
    (unless (= 0 (strncmp file-uri background file-uri-prefix-length))
      (set (deref error-string) "Unable to process picture-uri: uri type not supported")
      (g_free background)
      (return false))

    (snprintf wallpaper-out wallpaper-out-size "%s" (+ background file-uri-prefix-length))
    (g_free background)

    ;; GLib puts %20 for e.g. space in strings. Parse those out into valid file paths
    (var found-bad-char bool false)
    (each-char-in-string wallpaper-out current-char
      (when (= '%' (deref current-char))
        (set found-bad-char true)
        (break)))
    (when found-bad-char
      (uriUnescapeInPlaceEx wallpaper-out))
    (return true)))

 ('Windows
  (c-preprocessor-define WIN32_LEAN_AND_MEAN)
  (c-import "windows.h")

  (defun-local auto-color-get-current-background-filename (wallpaper-out (* char)
                                                           wallpaper-out-size (unsigned int)
                                                           error-string (* (* (const char)))
                                                           &return bool)
    (var buffer-size DWORD wallpaper-out-size)
    (var result DWORD
      (RegGetValueA HKEY_CURRENT_USER "Control Panel\\Desktop" "WallPaper" RRF_RT_REG_SZ
                    null wallpaper-out (addr buffer-size)))
    (unless (= result ERROR_SUCCESS)
      (set (deref error-string) "Could not get value from registry")
      (return false))
    (unless (at 0 wallpaper-out)
      (set (deref error-string) "The wallpaper registry value was retrieved, but it is empty")
      (return false))
    (return true))))

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
    (debug-print "error: failed to load %s with message: %s\n" image-to-load
                 (stbi_failure_reason))
    (return false))
  (set (path image-data-out > pixel-data) pixel-data)
  (return true))

;;
;; Color conversion
;;

(defmacro min (a any b any)
  (tokenize-push output
    (? (< (token-splice a) (token-splice b)) (token-splice a) (token-splice b)))
  (return true))

(def-type-alias-global auto-color ([] 3 (unsigned char)))
(defstruct auto-color-struct
  x (unsigned char)
  y (unsigned char)
  z (unsigned char))

;; Copied from HSL
(defun-local auto-color-get-lightness (color auto-color &return (unsigned char))
  (var max-component int 0)
  (var min-component int 255)
  (each-in-range 3 i
    (when (> (at i color) max-component)
      (set max-component (at i color)))
    (when (< (at i color) min-component)
      (set min-component (at i color))))
  (return (/ (+ max-component min-component) 2)))

;; ported by Renaud Bédard (@renaudbedard) from original code from Tanner Helland
;; http://www.tannerhelland.com/4435/convert-temperature-rgb-algorithm-code/
;; color space functions translated from HLSL versions on Chilli Ant (by Ian Taylor)
;; http://www.chilliant.com/rgb2hsv.html
;; licensed and released under Creative Commons 3.0 Attribution
;; https://creativecommons.org/licenses/by/3.0/
;; Copied from https://github.com/mixaal/imageprocessor.
;; Modified by converting to Cakelisp by Macoy Madson
(defun-local auto-color-clamp-zero-to-one (value float &return float)
  (when (< value 0.f) (return 0.f))
  (when (> value 1.0f) (return 1.0f))
  (return value))

(defstruct auto-color-float
  x float  ;; hue               red
  y float  ;; saturation   or   green
  z float) ;; lightness         blue

(defun-local auto-color-float-to-char (color auto-color-float &return auto-color-struct)
  (var converted auto-color-struct
    (array (type-cast (round (* (auto-color-clamp-zero-to-one (field color x)) 255)) (unsigned char))
           (type-cast (round (* (auto-color-clamp-zero-to-one (field color y)) 255)) (unsigned char))
           (type-cast (round (* (auto-color-clamp-zero-to-one (field color z)) 255)) (unsigned char))))
  (return converted))

(defun-local auto-color-char-to-float (color auto-color &return auto-color-float)
  (var converted auto-color-float
    (array (/ (at 0 color) 255.f)
           (/ (at 1 color) 255.f)
           (/ (at 2 color) 255.f)))
  (return converted))

(defun-local auto-color-hue-to-rgb (hue float &return auto-color-float)
  (var r float (auto-color-clamp-zero-to-one (- (fabs (- (* hue 6.0f) 3.0f)) 1.0f)))
  (var g float (auto-color-clamp-zero-to-one (- 2.0f (fabs (- (* hue 6.0f) 2.0f)))))
  (var b float (auto-color-clamp-zero-to-one (- 2.0f (fabs (- (* hue 6.0f) 4.0f)))))
  (var rgb auto-color-float (array r g b))
  (return rgb))

(defun-local auto-color-hsl-to-rgb (hsl auto-color-float &return auto-color-float)
  (var rgb auto-color-float (auto-color-hue-to-rgb (field hsl x)))
  (var c float (* (- 1.0f (fabs (- (* 2.0f (field hsl z)) 1.0f))) (field hsl y)))
  (set (field rgb x) (+ (* (- (field rgb x) 0.5f) c) (field hsl z)))
  (set (field rgb y) (+ (* (- (field rgb y) 0.5f) c) (field hsl z)))
  (set (field rgb z) (+ (* (- (field rgb z) 0.5f) c) (field hsl z)))

  (return rgb))

(var color-conversion-epsilon float 1e-10)

(defun-local auto-color-rgb-to-hcv (rgb auto-color-float &return auto-color-float)
  ;; Based on work by Sam Hocevar and Emil Persson
  (defstruct color-vec4 x float y float z float w float)
  (var p color-vec4)
  (var q color-vec4)
  (if (< (field rgb y) (field rgb z))
      (scope
       (set (field p x) (field rgb z))
       (set (field p y) (field rgb y))
       (set (field p z) -1.0f)
       (set (field p w) 2.0f/3.0f))
      (scope
       (set (field p x) (field rgb y))
       (set (field p y) (field rgb z))
       (set (field p z) 0.0f)
       (set (field p w) -1.0f/3.0f)))

  (if (< (field rgb x) (field p x))
      (scope
       (set (field q x) (field p x))
       (set (field q y) (field p y))
       (set (field q z) (field p w))
       (set (field q w) (field rgb x)))
      (scope
       (set (field q x) (field rgb x))
       (set (field q y) (field p y))
       (set (field q z) (field p z))
       (set (field q w) (field p x))))
  (var c float (- (field q x) (? (< (field q w) (field q y))
                                 (field q w) (field q y))))
  (var h float (fabs (+
                      (/ (- (field q w) (field q y))
                         (+ (* 6.0f c) color-conversion-epsilon))
                      (field q z))))
  (return (array h c (field q x))))

(defun-local auto-color-rgb-to-hsl (rgb auto-color-float &return auto-color-float)
  (var HCV auto-color-float (auto-color-rgb-to-hcv rgb))
  (var L float (- (field HCV z) (* (field HCV y) 0.5f)))
  (var S float (/ (field HCV y)
                  (+ (- 1.0f (fabs (- (* L 2.0f) 1.0f)))
                     color-conversion-epsilon)))
  (return (array (field HCV x) S L)))

(defun-local test--auto-color-conversions (&return int)
  (var test-hsl auto-color-float (array 0.25f 0.8f 0.2f))
  (var test-char-hsl auto-color-struct (auto-color-float-to-char test-hsl))
  (var color-rgb auto-color-float (auto-color-hsl-to-rgb test-hsl))
  (var color-char-rgb auto-color-struct (auto-color-float-to-char color-rgb))
  (var color-char-hsl auto-color-struct (auto-color-float-to-char
                                         (auto-color-rgb-to-hsl color-rgb)))
  (debug-print "
HSL:         %3d %3d %3d\n
RGB:         %3d %3d %3d\n
Back to HSL: %3d %3d %3d\n"
               (field test-char-hsl x)
               (field test-char-hsl y)
               (field test-char-hsl z)
               (field color-char-rgb x)
               (field color-char-rgb y)
               (field color-char-rgb z)
               (field color-char-hsl x)
               (field color-char-hsl y)
               (field color-char-hsl z))

  (when (!= 0 (memcmp (addr color-char-hsl) (addr test-char-hsl)
                      (sizeof (type auto-color-struct))))
    (return 1))
  (return 0))

;;
;; Color selection
;;

(defun-local auto-color-pick-colors-by-threshold (image-data (* auto-color-image)
                                                  color-palette-out (* auto-color)
                                                  num-colors-requested (unsigned char)
                                                  ;; Num colors attained
                                                  &return (unsigned char))
  (var color-samples ([] 512 auto-color))

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
   (debug-print "Samples: %dx%d for total of %d samples. Sample every %dx%d pixel
 of the %dx%d image\n"
                samples-per-x samples-per-y num-color-samples
                sample-skip-x sample-skip-y
                (path image-data > width) (path image-data > height)))

  (var current-sample-write (* auto-color) color-samples)
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
  (debug-print "Sampled %d pixels\n" num-samples)

  (var num-distinct-colors int 0)
  ;; TODO Dynamically adjust threshold based on whether we found enough colors?
  (var distinctness-threshold int 50)
  ;; Do color threshold selection
  (each-in-range num-samples sample-index
    (var is-distinct bool true)
    ;; TODO Add brightness/darkness filter like schemer2?
    (each-in-range num-distinct-colors distinct-color-index
      ;; Use int to prevent underflow
      (var color-difference ([] 3 int))
      (each-in-range 3 i
        (set (at i color-difference)
             (abs (- (type-cast (at sample-index i color-samples) int)
                     (type-cast (at distinct-color-index i color-palette-out) int)))))
      (var total-difference int 0)
      (each-in-range 3 i
        (set total-difference (+ total-difference (at i color-difference))))
      (unless (>= total-difference distinctness-threshold)
        (set is-distinct false)
        (break)))
    (unless is-distinct ;; Already represented
      (continue))

    ;; Color is distinct
    (each-in-range 3 i
      (set (at num-distinct-colors i color-palette-out)
           (at sample-index i color-samples)))

    (incr num-distinct-colors)
    ;; TODO: Consider what to do to not bias color selection to the first samples. Shuffle pixels?
    ;; Evict distinct colors randomly? Pick more colors than requested, then randomly narrow?
    (when (>= num-distinct-colors num-colors-requested)
      (break)))

  (return num-distinct-colors))

(defmacro set-color (dest any src any)
  (tokenize-push output
    (memcpy (token-splice dest) (token-splice src) (sizeof (type auto-color))))
  (return true))

(defun-local auto-color-sort-hsl-color-float-darkest-first (a (* (const void)) b (* (const void))
                                                            &return int)
  (var-cast-to a-value (* auto-color-float) a)
  (var-cast-to b-value (* auto-color-float) b)
  ;; Lightness
  (when (!= (path a-value > z) (path b-value > z))
    (return (? (< (path a-value > z) (path b-value > z)) -1 1)))
  ;; Saturation
  (when (!= (path a-value > y) (path b-value > y))
    (return (? (< (path a-value > y) (path b-value > y)) -1 1)))
  ;; Hue
  (return (? (< (path a-value > x) (path b-value > x)) -1 1)))

(defun-local auto-color-is-within-contrast-range (color auto-color-float
                                                  background-lightness float
                                                  minimum-contrast float
                                                  maximum-contrast float
                                                  &return bool)
  (var contrast float (- (field color z) background-lightness))
  (when (< contrast minimum-contrast)
    (return false))
  (when (> contrast maximum-contrast)
    (return false))
  (return true))

(defun-local auto-color-clamp-within-contrast-range (color auto-color-float
                                                     background-lightness float
                                                     minimum-contrast float
                                                     maximum-contrast float
                                                     &return auto-color-float)
  (var contrast float (- (field color z) background-lightness))
  (when (< contrast minimum-contrast)
    (set (field color z) (+ (field color z) (- minimum-contrast contrast))))
  (when (> contrast maximum-contrast)
    (set (field color z) (- (field color z) (- contrast maximum-contrast))))
  (return color))

;; Base16 is a style of colors supported by various apps. See https://github.com/chriskempson/base16
;; We need to pick colors from our palette (modifying them if necessary) in order to give the user
;; a good Base16 theme. Good being, high enough contrast, desired dark/light, different colors for
;; different things, and still reflecting the palette.
;; work-space should have length equal to num-colors-in-palette. This exists so this function
;; doesn't have to allocate memory.
(defun-local auto-color-create-base16-theme-from-colors (color-palette (* auto-color)
                                                         num-colors-in-palette (unsigned char)
                                                         work-space (* auto-color-float)
                                                         base16-colors-out (* auto-color))
  ;; Constants
  ;; These values ensure the backgrounds are nice and dark, even if the color palette values are all bright
  ;; Each background gets progressively lighter. We'll define a different max acceptible value for each level
  ;; For example, the Base00 default background is darkest, so it will be clamped to 0.08 if necessary
  (var maximum-background-brightness-thresholds ([] float) (array 0.08f 0.15f 0.2f 0.25f 0.3f 0.4f 0.45f))

  ;; Foreground contrasts (i.e. text color HSL lightness - background color HSL lightness)
  ;; These are relative values instead of ratios because you can't figure a ratio on a black background
  (var minimum-de-emphasized-text-contrast float 0.3f)
  (var minimum-text-contrast float 0.43f)
  ;; So as to not have too brilliant of colors dominating parts of the theme
  (var maximum-text-contrast float 0.65f)

  ;; Types
  (defenum auto-color-selection-method
    pick-darkest-color-force-dark-threshold
    pick-darkest-high-contrast-color-unique
    pick-high-contrast-bright-color-unique)

  (defstruct auto-color-base16-color
    description (* (const char))
    method auto-color-selection-method)

  (var selection-methods ([] 16 auto-color-base16-color)
    (array
     (array "base00 - Default Background"
            pick-darkest-color-force-dark-threshold)
     (array "base01 - Lighter Background (Used for status bars)"
            pick-darkest-color-force-dark-threshold)
     (array "base02 - Selection Background"
            pick-darkest-color-force-dark-threshold)
     (array "base03 - Comments, Invisibles, Line Highlighting"
            pick-darkest-high-contrast-color-unique)
     (array "base04 - Dark Foreground (Used for status bars)"
            pick-darkest-high-contrast-color-unique)
     (array "base05 - Default Foreground, Caret, Delimiters, Operators"
            pick-darkest-high-contrast-color-unique)
     (array "base06 - Light Foreground (Not often used)"
            pick-darkest-color-force-dark-threshold)
     (array "base07 - Light Background (Not often used)"
            pick-darkest-color-force-dark-threshold)
     (array "base08 - Variables, XML Tags, Markup Link Text, Markup Lists, Diff Deleted"
            pick-high-contrast-bright-color-unique)
     (array "base09 - Integers, Boolean, Constants, XML Attributes, Markup Link Url"
            pick-high-contrast-bright-color-unique)
     (array "base0A - Classes, Markup Bold, Search Text Background"
            pick-high-contrast-bright-color-unique)
     (array "base0B - Strings, Inherited Class, Markup Code, Diff Inserted"
            pick-high-contrast-bright-color-unique)
     (array "base0C - Support, Regular Expressions, Escape Characters, Markup Quotes"
            pick-high-contrast-bright-color-unique)
     (array "base0D - Functions, Methods, Attribute IDs, Headings"
            pick-high-contrast-bright-color-unique)
     (array "base0E - Keywords, Storage, Selector, Markup Italic, Diff Changed"
            pick-high-contrast-bright-color-unique)
     (array "base0F - Deprecated, Opening/Closing Embedded Language Tags, e.g. <?php ?>"
            pick-high-contrast-bright-color-unique)))

  ;; Prepare workspace by copying palette and sorting by lightness
  ;; Need to increase size if the dark backgrounds requests increase
  (each-in-range num-colors-in-palette i
    (var color-to-float auto-color-float
      (auto-color-char-to-float (at i color-palette)))
    (set (at i work-space)
         (auto-color-rgb-to-hsl color-to-float)))

  (qsort work-space
         num-colors-in-palette
         (sizeof (at 0 work-space))
         auto-color-sort-hsl-color-float-darkest-first)

  (debug-print "\nColors by lightness, darkest first:\n")
  (each-in-range num-colors-in-palette i
    (var color-rgb auto-color-float (auto-color-hsl-to-rgb (at i work-space)))
    (var color-char-rgb auto-color-struct (auto-color-float-to-char color-rgb))
    (debug-print "#%02x%02x%02x\t\t%f lightness (hsl %f %f %f)\n"
                 (field color-char-rgb x)
                 (field color-char-rgb y)
                 (field color-char-rgb z)
                 (field (at i work-space) z)
                 (field (at i work-space) x)
                 (field (at i work-space) y)
                 (field (at i work-space) z)))
  (debug-print "\n")

  (var next-unique-dark-color-index (unsigned char) 0)
  (var next-unique-dark-foreground-color-index (unsigned char) 0)
  (var next-unique-light-foreground-color-index (unsigned char) (- num-colors-in-palette 1))
  (var background-lightness float -1.f)

  (each-in-array selection-methods current-base
    (var selection-method auto-color-selection-method
      (field (at current-base selection-methods) method))
    (cond
      ((= pick-darkest-color-force-dark-threshold selection-method)
       (var clamped-color auto-color-float (at next-unique-dark-color-index work-space))
       ;; Keep it darker than thresholds
       (set (field clamped-color z)
            (min (field clamped-color z)
                 (at next-unique-dark-color-index
                     maximum-background-brightness-thresholds)))
       (when (= -1.f background-lightness)
         (set background-lightness (field clamped-color z)))
       (var dark-color auto-color-struct
         (auto-color-float-to-char
          (auto-color-hsl-to-rgb clamped-color)))
       (set-color (at current-base base16-colors-out) (addr dark-color))
       (incr next-unique-dark-color-index))

      ((= pick-darkest-high-contrast-color-unique selection-method)
       ;; TODO: Handle case where no color is selected
       (each-in-interval next-unique-dark-foreground-color-index
           num-colors-in-palette i
         (unless (auto-color-is-within-contrast-range
                  (at i work-space) background-lightness
                  minimum-de-emphasized-text-contrast
                  maximum-text-contrast)
           (continue))
         (var de-emphasized-color auto-color-struct
           (auto-color-float-to-char
            (auto-color-hsl-to-rgb (at i work-space))))
         (set-color (at current-base base16-colors-out) (addr de-emphasized-color))
         (set next-unique-dark-foreground-color-index (+ 1 i))
         (break)))

      ((= pick-high-contrast-bright-color-unique selection-method)
       (var clamped-color auto-color-float
         (auto-color-clamp-within-contrast-range
          (at next-unique-light-foreground-color-index work-space)
          background-lightness
          minimum-text-contrast
          maximum-text-contrast))
       (var foreground-color auto-color-struct
         (auto-color-float-to-char
          (auto-color-hsl-to-rgb clamped-color)))
       (set-color (at current-base base16-colors-out) (addr foreground-color))
       (decr next-unique-light-foreground-color-index)
       (when (< next-unique-light-foreground-color-index 0)
         (set next-unique-light-foreground-color-index 0))))

    (debug-print "#%02x%02x%02x\t\t%s\n"
                 (at 0 (at current-base base16-colors-out))
                 (at 1 (at current-base base16-colors-out))
                 (at 2 (at current-base base16-colors-out))
                 (field (at current-base selection-methods) description))))

;;
;; Interface
;;

;; base16-colors-out must be size 16
(defun auto-color-pick-from-current-background (base16-colors-out (* auto-color) &return bool)
  (var error-string (* (const char)) null)
  (var background-filename ([] 1024 char) (array 0))
  (unless (auto-color-get-current-background-filename
           background-filename (sizeof background-filename) (addr error-string))
    (debug-print "error: %s" error-string)
    (return false))
  (debug-print "\nPicking colors from '%s'\n" background-filename)

  (var image-data auto-color-image (array 0))
  (unless (auto-color-load-image background-filename (addr image-data))
    (return false))

  (var color-palette ([] 16 auto-color))
  (var num-colors-requested (unsigned char) (array-size color-palette))
  (var num-colors-attained (unsigned char)
    (auto-color-pick-colors-by-threshold (addr image-data) color-palette num-colors-requested))

  (each-in-range num-colors-attained i
    (debug-print "#%02x%02x%02x\n" (at i 0 color-palette)
                 (at i 1 color-palette)
                 (at i 2 color-palette)))

  (var work-space ([] 16 auto-color-float))
  (auto-color-create-base16-theme-from-colors
   color-palette num-colors-attained work-space
   base16-colors-out)

  (auto-color-image-destroy (addr image-data))
  (return true))
