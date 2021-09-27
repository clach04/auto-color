(import "AutoColor.cake")
(c-import "<stdio.h>")

(defun main (num-arguments int arguments ([] (* char)) &return int)
  (fprintf stderr "Hello, world!\n")
  (auto-color-pick-from-current-background)
  (return 0))
