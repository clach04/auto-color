(import "AutoColor.cake")
(c-import "<stdio.h>" "<string.h>")

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
OF THE POSSIBILITY OF SUCH DAMAGE.#"#)

(defun main (num-arguments int arguments ([] (* char)) &return int)
  (set g-auto-color-should-print true)
  (when (> num-arguments 1)
    (when (= 0 (strcmp (at 1 arguments) "--license"))
      (fprintf stderr "%s\n" g-copyright-string)
      (return 0)))
  (fprintf stderr "Auto Color\nCopyright (c) 2021 Macoy Madson.\n
Pass --license to see copyright and license info.\n")

  (var base16-colors ([] 16 auto-color) (array 0))
  (unless (auto-color-pick-from-current-background base16-colors)
    (return 1))
  (return 0))

(set-cakelisp-option executable-output "auto-color")
