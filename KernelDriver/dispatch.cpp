#include "imports.h"
#include "structs.h"

NTSTATUS KdReadProcessMemory(HANDLE ProcessId, PVOID SourceAddress, PVOID TargetAddress, SIZE_T Size)
{
	NTSTATUS Status;
	PEPROCESS SourceProcess;
	if (NT_SUCCESS(Status = PsLookupProcessByProcessId(ProcessId, &SourceProcess))) {
		__try {
			ProbeForRead(SourceAddress, Size, 1);
			ProbeForWrite(TargetAddress, Size, 1);
		}
		__except (EXCEPTION_EXECUTE_HANDLER) {
			ObDereferenceObject(SourceProcess);
			return STATUS_ACCESS_VIOLATION;
		}

		SIZE_T Result = 0;
		MmCopyVirtualMemory(SourceProcess, SourceAddress, PsGetCurrentProcess(), TargetAddress, Size, KernelMode, &Result);

		ObDereferenceObject(SourceProcess);
	}
	return Status;
}

NTSTATUS KdWriteProcessMemory(HANDLE ProcessId, PVOID SourceAddress, PVOID TargetAddress, SIZE_T Size)
{
	NTSTATUS Status;
	PEPROCESS TargetProcess;

	if (NT_SUCCESS(Status = PsLookupProcessByProcessId(ProcessId, &TargetProcess))) {
		__try {
			ProbeForRead(SourceAddress, Size, 1);
			ProbeForWrite(TargetAddress, Size, 1);
		}
		__except (EXCEPTION_EXECUTE_HANDLER) {
			ObDereferenceObject(TargetProcess);
			return STATUS_ACCESS_VIOLATION;
		}

		SIZE_T Result = 0;
		MmCopyVirtualMemory(PsGetCurrentProcess(), SourceAddress, TargetProcess, TargetAddress, Size, KernelMode, &Result);

		ObDereferenceObject(TargetProcess);
	}

	return Status;
}

NTSTATUS KdGetProcessInfo(HANDLE ProcessId, OUT PBOOLEAN IsWow64, OUT PVOID* PebBaseAddress) {
	NTSTATUS Status;
	PEPROCESS Process;

	if (NT_SUCCESS(Status = PsLookupProcessByProcessId(ProcessId, &Process))) {

		PPEB pPEB = PsGetProcessPeb(Process);
		PPEB32 pPEB32 = (PPEB32)PsGetProcessWow64Process(Process);

		if (pPEB32) {
			DbgPrintEx(0, 0, "Getting 32bit process PEB, %X\n", (PVOID)pPEB32);
			*IsWow64 = TRUE;
			*PebBaseAddress = (PVOID)pPEB32;
		}
		else if (pPEB) {
			DbgPrintEx(0, 0, "Getting 64bit process PEB, %X\n", (PVOID)pPEB);
			*IsWow64 = FALSE;
			*PebBaseAddress = (PVOID)pPEB;
		}

		ObDereferenceObject(Process);
	}

	return Status;
}