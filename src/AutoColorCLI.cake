(import "AutoColor.cake")
(c-import "<stdio.h>")

(var g-copyright-string (* (const char))
  #"#Auto Color
Created by Macoy Madson <macoy@macoy.me>.
https://macoy.me/code/macoy/file-helper
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

Uses color conversion functions with the following preamble:
Ported by Renaud Bédard (@renaudbedard) from original code from Tanner Helland:
http://www.tannerhelland.com/4435/convert-temperature-rgb-algorithm-code/
Color space functions translated from HLSL versions on Chilli Ant (by Ian Taylor):
http://www.chilliant.com/rgb2hsv.html
Licensed and released under Creative Commons 3.0 Attribution:
https://creativecommons.org/licenses/by/3.0/
Copied from https://github.com/mixaal/imageprocessor.
Modified by converting to Cakelisp by Macoy Madson.#"#)

;; (defun-local print-help ()
;;   (fprintf stderr "Expected filename\n"))

(defun main (num-arguments int arguments ([] (* char)) &return int)
  ;; (unless (> 1 num-arguments)
  ;;   (print-help)
  ;;   (return 1))
  (fprintf stderr "%s\n" g-copyright-string)
  (auto-color-pick-from-current-background)
  (return 0))

(set-cakelisp-option executable-output "auto-color")
