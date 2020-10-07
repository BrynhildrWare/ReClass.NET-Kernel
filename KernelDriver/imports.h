#pragma once
#include <ntdef.h>
#include <ntifs.h>
#include <windef.h>

extern "C" {
	NTKERNELAPI NTSTATUS MmCopyVirtualMemory
	(
		IN	PEPROCESS		SourceProcess,
		IN	PVOID			SourceAddress,
		IN	PEPROCESS		TargetProcess,
		OUT PVOID			TargetAddress,
		IN	SIZE_T			BufferSize,
		IN	KPROCESSOR_MODE	PreviousMode,
		OUT PSIZE_T			ReturnSize
	);

	NTKERNELAPI PPEB NTAPI PsGetProcessPeb
	(
		IN PEPROCESS Process
	);

	NTKERNELAPI PVOID NTAPI	PsGetProcessWow64Process
	(
		IN PEPROCESS Process
	);

	NTKERNELAPI NTSTATUS NTAPI PsReferenceProcessFilePointer
	(
		IN PEPROCESS        Process,
		OUT PFILE_OBJECT*	FileObject
	);
}
