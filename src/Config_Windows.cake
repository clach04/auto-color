(skip-build)
(set-cakelisp-option cakelisp-src-dir "Dependencies/cakelisp/src")
(set-cakelisp-option cakelisp-lib-dir "Dependencies/cakelisp/bin")
(add-cakelisp-search-directory "Dependencies/gamelib/src")
(add-cakelisp-search-directory "Dependencies/cakelisp/runtime")

;; For bootstrap build only
(add-c-search-directory-global "Dependencies/cakelisp/src")
(add-cakelisp-search-directory "Dependencies/cakelisp")

(add-cakelisp-search-directory "src")

(comptime-define-symbol 'Windows)

;; Remedybg
;; (add-linker-options "/DEBUG:FULL")
;; (add-build-options "/Zi" "/FS")

(set-cakelisp-option executable-output "auto-color.exe")
