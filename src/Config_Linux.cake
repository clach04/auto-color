(skip-build)
(set-cakelisp-option cakelisp-src-dir "Dependencies/cakelisp/src")
(add-cakelisp-search-directory "Dependencies/gamelib/src")
(add-cakelisp-search-directory "Dependencies/cakelisp/runtime")

(add-cakelisp-search-directory "src")

(comptime-define-symbol 'Unix)

;; Uncomment for profiling
;; (comptime-define-symbol 'Profile)
;; (comptime-define-symbol 'Release)

(comptime-cond
 ('Profile
  (add-build-options-global "-O3")
  (import &comptime-only "ProfilerAutoInstrument.cake"))
 ('Release
  (add-build-config-label "Release")
  (add-build-options-global "-O3")))
