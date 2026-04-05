/**
 * @name Log injection vulnerabilities
 * @description Finds potential log injection vulnerabilities where user input is logged without sanitization
 * @kind problem
 * @problem.severity error
 * @security-severity 7.8
 * @id cs/log-injection
 * @tags security
 */

import csharp
import semmle.code.csharp.security.dataflow.LogInjection

from MethodCall logCall, Expr userInput
where 
  logCall.getTarget().hasName("LogInformation") or
  logCall.getTarget().hasName("LogError") or
  logCall.getTarget().hasName("LogWarning") or
  logCall.getTarget().hasName("LogDebug") or
  logCall.getTarget().hasName("LogTrace") and
  exists(DataFlow::PathNode source, DataFlow::PathNode sink |
    source.getNode() = userInput and
    sink.getNode() = logCall.getAnArgument() and
    DataFlow::localPath(source, sink) and
    not exists(MethodCall sanitizeCall |
      sanitizeCall.getTarget().hasName("Sanitize") or
      sanitizeCall.getTarget().hasName("MaskEmail") or
      sanitizeCall.getTarget().hasName("MaskToken") or
      sanitizeCall.getTarget().hasName("MaskServerKey") and
      sanitizeCall.getAnArgument() = userInput
    )
  )
select logCall, userInput, "Log entry created from user input without sanitization"
