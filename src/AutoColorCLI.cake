(import "AutoColor.cake")
(c-import "<stdio.h>" "<string.h>")

(defun main (num-arguments int arguments (array (addr char)) &return int)
  (set g-auto-color-should-print true)
  (when (> num-arguments 1)
    (when (= 0 (strcmp (at 1 arguments) "--license"))
      (fprintf stderr "%s\n" g-auto-color-copyright-string)
      (return 0)))
  (fprintf stderr "Auto Color\nCopyright (c) 2021 Macoy Madson.\n
Pass --license to see copyright and license info.\n")

  (var base16-colors (array 16 auto-color) (array 0))
  (unless (auto-color-pick-from-current-background base16-colors)
    (return 1))
  (return 0))

(set-cakelisp-option executable-output "auto-color")
