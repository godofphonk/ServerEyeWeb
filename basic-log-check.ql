/**
 * @name Basic log check
 * @description Find logger method calls
 * @kind problem
 * @problem.severity info
 * @id cs/basic-log
 * @tags security
 */

from Method m
where 
  m.getName().matches("Log%") and
  m.getDeclaringType().getName().matches("%Logger%")
select m, "Found logger method call"
