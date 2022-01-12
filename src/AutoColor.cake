(import "Image.cake"
        &comptime-only "CHelpers.cake")
(c-import "<stdio.h>" ;; fprintf, strncmp
          "<stdlib.h>" ;; qsort
          "<string.h>" ;; memcmp
          "<math.h>" ;; sqrtf, round
          &with-decls "stdbool.h") ;; bool

(var-global g-auto-color-copyright-string (* (const char))
            #"#Auto Color
Created by Macoy Madson <macoy@macoy.me>.
https://macoy.me/code/macoy/auto-color
Copyright (c) 2021 Macoy Madson.

Auto Color is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Auto Color is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Auto Color.  If not, see <https://www.gnu.org/licenses/>.

--------------------------------------------------------------------------------

Uses stb_image:
Copyright (c) 2017 Sean Barrett
Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

--------------------------------------------------------------------------------

Uses modified color conversion functions with the following preamble:
Ported by Renaud Bédard (@renaudbedard) from original code from Tanner Helland:
http://www.tannerhelland.com/4435/convert-temperature-rgb-algorithm-code/
Color space functions translated from HLSL versions on Chilli Ant (by Ian Taylor):
http://www.chilliant.com/rgb2hsv.html
Licensed and released under Creative Commons 3.0 Attribution:
https://creativecommons.org/licenses/by/3.0/

Copied from https://github.com/mixaal/imageprocessor.
Modified by Macoy Madson.

--------------------------------------------------------------------------------

Uses modified code from uriparser:
uriparser - RFC 3986 URI parsing library

Copyright (C) 2007, Weijia Song <songweijia@gmail.com>
Copyright (C) 2007, Sebastian Pipping <sebastian@pipping.org>
All rights reserved.

Redistribution and use in source  and binary forms, with or without
modification, are permitted provided  that the following conditions
are met:

    1. Redistributions  of  source  code   must  retain  the  above
       copyright notice, this list  of conditions and the following
       disclaimer.

    2. Redistributions  in binary  form  must  reproduce the  above
       copyright notice, this list  of conditions and the following
       disclaimer  in  the  documentation  and/or  other  materials
       provided with the distribution.

    3. Neither the  name of the  copyright holder nor the  names of
       its contributors may be used  to endorse or promote products
       derived from  this software  without specific  prior written
       permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND  ANY EXPRESS OR IMPLIED WARRANTIES,  INCLUDING, BUT NOT
LIMITED TO,  THE IMPLIED WARRANTIES OF  MERCHANTABILITY AND FITNESS
FOR  A  PARTICULAR  PURPOSE  ARE  DISCLAIMED.  IN  NO  EVENT  SHALL
THE  COPYRIGHT HOLDER  OR CONTRIBUTORS  BE LIABLE  FOR ANY  DIRECT,
INDIRECT, INCIDENTAL, SPECIAL,  EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO,  PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA,  OR PROFITS; OR BUSINESS INTERRUPTION)
HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT  LIABILITY,  OR  TORT (INCLUDING  NEGLIGENCE  OR  OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED
OF THE POSSIBILITY OF SUCH DAMAGE.

--------------------------------------------------------------------------------

ON UBUNTU LINUX ONLY: Uses GNOME GLib and GIO:

Copyright (C) GTK Development Team

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, see <http://www.gnu.org/licenses/>.
#"#)

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
  ;; For glibconfig.h, which is auto-generated for each platform
  (add-c-search-directory-module "/usr/lib/x86_64-linux-gnu/glib-2.0/include")
  (add-c-search-directory-module "/usr/lib/aarch64-linux-gnu/glib-2.0/include")
  (c-import "gio/gio.h")
  (add-library-dependency "glib-2.0" "gio-2.0" "gobject-2.0")

  (add-c-build-dependency "UriUnescape.c")

  (declare-extern-function uriUnescapeInPlaceEx (in-out (* char) &return (* (const char))))

  (defun auto-color-get-current-background-filename (wallpaper-out (* char)
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

  (defun auto-color-get-current-background-filename (wallpaper-out (* char)
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
    (return true))

  (add-linker-options "advapi32.lib")))

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
  (var packed-color auto-color-float (array h c (field q x)))
  (return packed-color))

(defun-local auto-color-rgb-to-hsl (rgb auto-color-float &return auto-color-float)
  (var HCV auto-color-float (auto-color-rgb-to-hcv rgb))
  (var L float (- (field HCV z) (* (field HCV y) 0.5f)))
  (var S float (/ (field HCV y)
                  (+ (- 1.0f (fabs (- (* L 2.0f) 1.0f)))
                     color-conversion-epsilon)))
  (var packed-color auto-color-float (array (field HCV x) S L))
  (return packed-color))

(defun test--auto-color-conversions (&return int)
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
  (unless (and image-data color-palette-out num-colors-requested) (return 0))

  (var color-samples ([] 512 auto-color))

  ;; TODO This isn't ideal. An even multiple would end up only sampling the diagonal, etc.
  (var num-samples-requested int (array-size color-samples))
  (var num-pixels int (* (path image-data > width) (path image-data > height)))
  (var pixel-skip int
    (/ num-pixels num-samples-requested))
  (when (< num-pixels num-samples-requested)
    (set pixel-skip 1))
  (debug-print "Samples: Sample every %d pixel for a total of %d samples. Image is %dx%d\n"
               pixel-skip (/ num-pixels pixel-skip)
               (path image-data > width) (path image-data > height))

  (var current-sample-write (* auto-color) color-samples)
  (c-for (var current-pixel int pixel-skip) (< current-pixel num-pixels)
      (set current-pixel (+ current-pixel pixel-skip))
    (var pixel-color-index int (* 3 current-pixel))
    (var pixel-color-component (* (unsigned char))
      (addr (at pixel-color-index (path image-data > pixel-data))))
    (each-in-range 3 i
      (set (at i (deref current-sample-write)) (at i pixel-color-component)))
    (incr current-sample-write))

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
  (unless (and color-palette num-colors-in-palette work-space base16-colors-out) (return))

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

  ;; Each time a dark background color is re-used, add this much brightness
  (var darkest-brighten-per-repeat float 0.03f)
  (var dark-brighten-per-repeat float 0.07f)
  (var light-darken-per-repeat float 0.01f)

  ;; Types
  (defenum auto-color-selection-method
    pick-darkest-color-force-dark-threshold
    pick-darkest-high-contrast-color
    pick-high-contrast-bright-color)

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
            pick-darkest-high-contrast-color)
     (array "base04 - Dark Foreground (Used for status bars)"
            pick-darkest-high-contrast-color)
     (array "base05 - Default Foreground, Caret, Delimiters, Operators"
            pick-darkest-high-contrast-color)
     (array "base06 - Light Foreground (Not often used)"
            pick-darkest-color-force-dark-threshold)
     (array "base07 - Light Background (Not often used)"
            pick-darkest-color-force-dark-threshold)
     (array "base08 - Variables, XML Tags, Markup Link Text, Markup Lists, Diff Deleted"
            pick-high-contrast-bright-color)
     (array "base09 - Integers, Boolean, Constants, XML Attributes, Markup Link Url"
            pick-high-contrast-bright-color)
     (array "base0A - Classes, Markup Bold, Search Text Background"
            pick-high-contrast-bright-color)
     (array "base0B - Strings, Inherited Class, Markup Code, Diff Inserted"
            pick-high-contrast-bright-color)
     (array "base0C - Support, Regular Expressions, Escape Characters, Markup Quotes"
            pick-high-contrast-bright-color)
     (array "base0D - Functions, Methods, Attribute IDs, Headings"
            pick-high-contrast-bright-color)
     (array "base0E - Keywords, Storage, Selector, Markup Italic, Diff Changed"
            pick-high-contrast-bright-color)
     (array "base0F - Deprecated, Opening/Closing Embedded Language Tags, e.g. <?php ?>"
            pick-high-contrast-bright-color)))

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

  (defstruct color-selection-state
    next-index int
    ;; Handle edge case where same color needs to be used
    num-repeat-uses (unsigned char)
    num-colors-this-method (unsigned char))
  (var dark-color-state color-selection-state (array 0 0 0))
  (var dark-foreground-color-state color-selection-state (array (/ num-colors-in-palette 2) 0 0))
  (var light-foreground-color-state color-selection-state (array (- num-colors-in-palette 1) 0 0))

  (each-in-array selection-methods current-base
    (var selection-method auto-color-selection-method
      (field (at current-base selection-methods) method))
    (cond
      ((= pick-darkest-color-force-dark-threshold selection-method)
       (incr (field dark-color-state num-colors-this-method)))
      ((= pick-darkest-high-contrast-color selection-method)
       (incr (field dark-foreground-color-state num-colors-this-method)))
      ((= pick-high-contrast-bright-color selection-method)
       (incr (field light-foreground-color-state num-colors-this-method)))))

  (var background-lightness float -1.f)

  (each-in-array selection-methods current-base
    (var selection-method auto-color-selection-method
      (field (at current-base selection-methods) method))
    (cond
      ((= pick-darkest-color-force-dark-threshold selection-method)
       (var clamped-color auto-color-float (at (field dark-color-state next-index) work-space))
       (when (field dark-color-state num-repeat-uses)
         (set (field clamped-color z)
              (* (at (field dark-color-state next-index) maximum-background-brightness-thresholds)
                 (/ (field dark-color-state num-repeat-uses)
                    (type-cast (field dark-color-state num-colors-this-method) float)))))

       ;; Keep it darker than thresholds
       (set (field clamped-color z)
            (min (field clamped-color z)
                 (at (field dark-color-state next-index)
                     maximum-background-brightness-thresholds)))
       (when (= -1.f background-lightness)
         (set background-lightness (field clamped-color z)))
       (var dark-color auto-color-struct
         (auto-color-float-to-char
          (auto-color-hsl-to-rgb clamped-color)))
       (set-color (at current-base base16-colors-out) (addr dark-color))
       (incr (field dark-color-state next-index))
       (when (>= (field dark-color-state next-index) num-colors-in-palette)
         (incr (field dark-color-state num-repeat-uses))
         (set (field dark-color-state next-index) (- num-colors-in-palette 1))))

      ((= pick-darkest-high-contrast-color selection-method)
       (var clamped-color auto-color-float
         (at (field dark-foreground-color-state next-index) work-space))
       (when (field dark-foreground-color-state num-repeat-uses)
         (set (field clamped-color z)
              (+ minimum-de-emphasized-text-contrast
                 (* (- maximum-text-contrast minimum-de-emphasized-text-contrast)
                    (/ (field dark-foreground-color-state num-repeat-uses)
                       (type-cast (field dark-foreground-color-state num-colors-this-method) float))))))
       (set clamped-color (auto-color-clamp-within-contrast-range
                           clamped-color
                           background-lightness
                           minimum-de-emphasized-text-contrast
                           maximum-text-contrast))
       (var de-emphasized-color auto-color-struct
         (auto-color-float-to-char
          (auto-color-hsl-to-rgb clamped-color)))
       (set-color (at current-base base16-colors-out) (addr de-emphasized-color))
       (incr (field dark-foreground-color-state next-index))
       (when (>= (field dark-foreground-color-state next-index) num-colors-in-palette)
         (incr (field dark-foreground-color-state num-repeat-uses))
         (set (field dark-foreground-color-state next-index) (- num-colors-in-palette 1))))

      ((= pick-high-contrast-bright-color selection-method)
       (var clamped-color auto-color-float
         (at (field light-foreground-color-state next-index) work-space))
       (when (field light-foreground-color-state num-repeat-uses)
         (set (field clamped-color z)
              (+ minimum-text-contrast
                 (* (- maximum-text-contrast minimum-text-contrast)
                    (/ (field light-foreground-color-state num-repeat-uses)
                       (type-cast (field light-foreground-color-state num-colors-this-method) float))))))
       (set clamped-color
            (auto-color-clamp-within-contrast-range
             clamped-color
             background-lightness
             minimum-text-contrast
             maximum-text-contrast))
       (var foreground-color auto-color-struct
         (auto-color-float-to-char
          (auto-color-hsl-to-rgb clamped-color)))
       (set-color (at current-base base16-colors-out) (addr foreground-color))
       (if (> (field light-foreground-color-state next-index) 0)
           (decr (field light-foreground-color-state next-index))
           (incr (field light-foreground-color-state num-repeat-uses)))))

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
