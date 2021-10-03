(skip-build)

(defun-comptime create-single-file-lib (manager (& ModuleManager) link-command (& ProcessCommand)
                                        link-time-inputs (* ProcessCommandInput)
                                        num-link-time-inputs int
                                        &return bool)
  (var single-header-filename (* (const char)) "AutoColor.h")
  (var file-out (* FILE) (fopen single-header-filename "w"))
  (unless file-out
    (Log "error: failed to open single file header for writing\n")
    (return false))

  (fprintf file-out "// Hello World\n")

  (for-in module (* Module) (field manager modules)
    (var standard-files-to-output ([] (* (const char)))
      (array (call-on c_str (path module > headerOutputName))
             (call-on c_str (path module > sourceOutputName))))
    (each-in-array standard-files-to-output i
      (var source-filename (* (const char)) (at i standard-files-to-output))
      (unless (fileExists source-filename)
        (continue))

      (var module-file (* FILE) (fopen source-filename "r"))
      (unless module-file
        (Logf "error: failed to open module file %s\n" source-filename)
        (fclose file-out)
        (return false))
      (fseek module-file 0 SEEK_END)
      (var file-size size_t (ftell module-file))
      (rewind module-file)
      (var-cast-to contents-buffer (* char) (malloc (+ 1 file-size)))
      (fread contents-buffer file-size 1 module-file)
      (set (at file-size contents-buffer) 0)
      (fclose module-file)

      (fprintf file-out "\n/* %s */\n\n" \
               "%s"
               source-filename contents-buffer)
      (free contents-buffer)))

  (fclose file-out)

  ;; Format it, because I don't have perfect Cakelisp output yet
  (run-process-sequential-or ("clang-format" "-i" single-header-filename)
    ;; We won't make this fatal for now, because it's purely stylistic
    (Logf "warning: failed to run clang-format on single-header file %s\n"
          single-header-filename))

  (return true))

(add-compile-time-hook pre-link create-single-file-lib)
