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
  (add-library-dependency "glib-2.0" "gio-2.0")

  (defun-local auto-color-get-current-background-filename (error-string (* (* (const char)))
                                                           &return (* (const char)))
    (var g-settings (* GSettings) (g_settings_new "org.gnome.desktop.background"))
    (unless g-settings
      (set (deref error-string) "Unable to get GTK settings for org.gnome.desktop.background")
      (return null))
    (var background (* gchar) (g_settings_get_string g-settings "picture-uri"))
    (unless background
      (set (deref error-string) "Unable to get picture-uri from org.gnome.desktop.background")
      (return null))

    (var file-uri (* (const char)) "file://")
    (var file-uri-prefix-length size_t (strlen file-uri))
    (unless (= 0 (strncmp file-uri background file-uri-prefix-length))
      (set (deref error-string) "Unable to process picture-uri: uri type not supported")
      (return null))

    (set background (+ background file-uri-prefix-length))
    (return background))))

(defun auto-color-pick-from-file (image-to-load (* (const char)) &return bool)
  (var width int 0)
  (var height int 0)
  (var num-pixel-components int 0)
  (var num-desired-channels int 3)
  (var pixel-data (* (unsigned char))
    (stbi_load image-to-load (addr width) (addr height) (addr num-pixel-components)
               num-desired-channels))
  (unless pixel-data
    (fprintf stderr "error: failed to load %s with message: %s\n" image-to-load
             (stbi_failure_reason))
    (return false))

  (fprintf stderr "size of %s: %dx%d\n" image-to-load width height)
    (fprintf stderr "num components in %s: %d\n" image-to-load num-pixel-components)
    (fprintf stderr "first three pixels:\n")
    (each-in-range 3 i
      (var rgb-components (* (unsigned char)) (addr (at (* i 3) pixel-data)))
      (fprintf stderr "[%d] %d %d %d\n"
               i
               (at 0 rgb-components)
               (at 1 rgb-components)
               (at 2 rgb-components)))

  (return true))

(defun auto-color-pick-from-current-background (&return bool)
  (var error-string (* (const char)) null)
  (var background-filename (* (const char))
    (auto-color-get-current-background-filename (addr error-string)))
  (unless background-filename
    (fprintf stderr "error: %s" error-string)
    (return false))
  (fprintf stderr "\nPicking colors from '%s'\n" background-filename)

  (return (auto-color-pick-from-file background-filename)))
