# ReClass.NET-Kernel

A simple ReClass.NET plugin which allows you to read/write process memory without WINAPI using a driver.

## Why bother?

* The pre-existing plugins on Github provide the ability to work through driver. However, most of them are written in c++ while the ReClass.NET is, of course, made with C#.
* Numbers of those plugins do not enumerate process modules through driver but `CreateToolhelp32Snapshot`. This can be a problem with protected process (EAC, BE, etc.)

## Todo, or not todo?
- [ ] Implement enumerate sections
- [x] Didn't come up with more so far..

## Further information that you should be aware of
* This plugin was intended for 64-bit processes. However, it does support **Wow64** programs, though the reliability has not been tested. Check it yourself before use.
