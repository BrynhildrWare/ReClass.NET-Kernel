#pragma once
#include "imports.h"

NTSTATUS KdReadProcessMemory(HANDLE ProcessId, PVOID SourceAddress, PVOID TargetAddress, SIZE_T Size);
NTSTATUS KdWriteProcessMemory(HANDLE ProcessId, PVOID SourceAddress, PVOID TargetAddress, SIZE_T Size);
NTSTATUS KdGetProcessInfo(HANDLE ProcessId, OUT PBOOLEAN IsWow64, OUT PVOID* PebBaseAddress);