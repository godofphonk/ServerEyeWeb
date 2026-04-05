/**
 * @name Simple log injection check
 * @description Find logger calls with potential user input
 * @kind problem
 * @problem.severity warning
 * @id cs/simple-log-injection
 * @tags security
 */

import csharp

from MethodCall mc
where 
  mc.getTarget().hasName("LogInformation") or
  mc.getTarget().hasName("LogError") or
  mc.getTarget().hasName("LogWarning") or
  mc.getTarget().hasName("LogDebug") or
  mc.getTarget().hasName("LogTrace") and
  exists(Argument arg |
    arg = mc.getAnArgument() and
    (
      // Check for parameters that might contain user input
      arg.toString().matches("%Email%") or
      arg.toString().matches("%email%") or
      arg.toString().matches("%Token%") or
      arg.toString().matches("%token%") or
      arg.toString().matches("%State%") or
      arg.toString().matches("%state%") or
      arg.toString().matches("%Code%") or
      arg.toString().matches("%code%") or
      arg.toString().matches("%Action%") or
      arg.toString().matches("%action%")
    ) and
    // Exclude if already sanitized
    not exists(MethodCall sanitize |
      sanitize.getTarget().hasName("Sanitize") or
      sanitize.getTarget().hasName("MaskEmail") or
      sanitize.getTarget().hasName("MaskToken") or
      sanitize.getTarget().hasName("MaskServerKey")
    )
  )
select mc, "Potential log injection: user input may be logged without sanitization"
