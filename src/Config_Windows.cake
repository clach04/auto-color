(skip-build)
(set-cakelisp-option cakelisp-src-dir "Dependencies/cakelisp/src")
(add-cakelisp-search-directory "Dependencies/gamelib/src")
(add-cakelisp-search-directory "Dependencies/cakelisp/runtime")

;; For bootstrap build only
(add-c-search-directory-global "Dependencies/cakelisp/src")
(add-cakelisp-search-directory "Dependencies/cakelisp")

(add-cakelisp-search-directory "src")

(comptime-define-symbol 'Windows)
;; TODO Remove
;;(add-build-options-global "/Zi")
(add-linker-options "advapi32.lib")
