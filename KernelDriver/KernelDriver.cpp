#include "imports.h"
#include "structs.h"
#include "dispatch.h"

#define IO_READ_REQUEST CTL_CODE(FILE_DEVICE_UNKNOWN, 0xb52801, METHOD_BUFFERED, FILE_SPECIAL_ACCESS)
#define IO_WRITE_REQUEST CTL_CODE(FILE_DEVICE_UNKNOWN, 0xb52802, METHOD_BUFFERED, FILE_SPECIAL_ACCESS)
#define IO_PROCESS_INFO CTL_CODE(FILE_DEVICE_UNKNOWN, 0xb52803, METHOD_BUFFERED, FILE_SPECIAL_ACCESS)

PDEVICE_OBJECT DeviceObject;
UNICODE_STRING DeviceName, LinkName;

NTSTATUS IoControl(PDEVICE_OBJECT pDeviceObject, PIRP Irp)
{
	NTSTATUS Status;
	ULONG BytesIO = 0;

	PIO_STACK_LOCATION stack = IoGetCurrentIrpStackLocation(Irp);
	ULONG ControlCode = stack->Parameters.DeviceIoControl.IoControlCode;

	if (ControlCode == IO_READ_REQUEST)
	{
		PKERNEL_READ_REQUEST ReadInput = (PKERNEL_READ_REQUEST)Irp->AssociatedIrp.SystemBuffer;
		HANDLE ProcessId = (HANDLE)ReadInput->ProcessId;

		NTSTATUS Result = KdReadProcessMemory(ProcessId, ReadInput->Address, ReadInput->Buffer, ReadInput->Size);

		if (NT_SUCCESS(Result)) {
			//All good, do nothing
			DbgPrintEx(0, 0, "Read at address: %p (pid: %lu)\n", ReadInput->Address, (ULONG)ProcessId);
		}
		else {
			DbgPrintEx(0, 0, "Failed to read at address: %p (pid: %lu)\n", ReadInput->Address, (ULONG)ProcessId);
			DbgPrintEx(0, 0, "Reason: %X\n", Result);
		}

		Status = STATUS_SUCCESS;
		BytesIO = sizeof(KERNEL_READ_REQUEST);
	}
	else if (ControlCode == IO_WRITE_REQUEST)
	{
		PKERNEL_WRITE_REQUEST WriteInput = (PKERNEL_WRITE_REQUEST)Irp->AssociatedIrp.SystemBuffer;
		HANDLE ProcessId = (HANDLE)WriteInput->ProcessId;

		NTSTATUS Result = KdWriteProcessMemory(ProcessId, WriteInput->Buffer, WriteInput->Address, WriteInput->Size);

		if (NT_SUCCESS(Result)) {
			//All good, do nothing
			DbgPrintEx(0, 0, "Write at address: %p (pid: %lu)\n", WriteInput->Address, (ULONG)ProcessId);
		}
		else {
			DbgPrintEx(0, 0, "Failed to write at address: %p (pid: %lu)\n", WriteInput->Address, (ULONG)ProcessId);
			DbgPrintEx(0, 0, "Reason: %X\n", Result);
		}

		Status = STATUS_SUCCESS;
		BytesIO = sizeof(KERNEL_WRITE_REQUEST);
	}
	else if (ControlCode == IO_PROCESS_INFO)
	{
		PKERNEL_PROCESS_INFO PebInfo = (PKERNEL_PROCESS_INFO)Irp->AssociatedIrp.SystemBuffer;
		HANDLE ProcessId = (HANDLE)PebInfo->ProcessId;

		NTSTATUS Result = KdGetProcessInfo(ProcessId, &PebInfo->IsWow64, &PebInfo->PebAddress);

		if (NT_SUCCESS(Result)) {
			DbgPrintEx(0, 0, "Getting Peb Base Address at %X\n", PebInfo->PebAddress);
		}
		else {
			DbgPrintEx(0, 0, "Failed to get peb base\n");
			DbgPrintEx(0, 0, "Reason: %X\n", Result);
		}

		Status = STATUS_SUCCESS;
		BytesIO = sizeof(KERNEL_PROCESS_INFO);
	}
	else
	{
		Status = STATUS_INVALID_PARAMETER;
		BytesIO = 0;
	}

	Irp->IoStatus.Status = Status;
	Irp->IoStatus.Information = BytesIO;
	IoCompleteRequest(Irp, IO_NO_INCREMENT);

	return Status;
}

VOID UnloadDriver(PDRIVER_OBJECT pDriverObject)
{
	DbgPrintEx(0, 0, "Kernel Class Driver Unloaded\n");
	IoDeleteSymbolicLink(&LinkName);
	IoDeleteDevice(pDriverObject->DeviceObject);
}

NTSTATUS CreateCall(PDEVICE_OBJECT pDeviceObject, PIRP irp)
{
	irp->IoStatus.Status = STATUS_SUCCESS;
	irp->IoStatus.Information = 0;

	IoCompleteRequest(irp, IO_NO_INCREMENT);
	return STATUS_SUCCESS;
}

NTSTATUS CloseCall(PDEVICE_OBJECT pDeviceObject, PIRP irp)
{
	irp->IoStatus.Status = STATUS_SUCCESS;
	irp->IoStatus.Information = 0;

	IoCompleteRequest(irp, IO_NO_INCREMENT);
	return STATUS_SUCCESS;
}

extern "C" NTSTATUS DriverEntry(PDRIVER_OBJECT pDriverObject, PUNICODE_STRING pRegistryPath) {
	DbgPrintEx(0, 0, "Kernel Class Driver Loaded\n");

	RtlInitUnicodeString(&DeviceName, L"\\Device\\KernelClass");
	RtlInitUnicodeString(&LinkName, L"\\DosDevices\\KernelClass");

	IoCreateDevice(pDriverObject, 0, &DeviceName, FILE_DEVICE_UNKNOWN, FILE_DEVICE_SECURE_OPEN, FALSE, &DeviceObject);
	NTSTATUS Status = IoCreateSymbolicLink(&LinkName, &DeviceName);
	if (!NT_SUCCESS(Status)) {
		DbgPrintEx(0, 0, "IoCreateSymbolicLink Failed: %X\n", Status);
	}
	else {
		DbgPrintEx(0, 0, "IoCreateSymbolicLink Succeed\n");
	}

	pDriverObject->MajorFunction[IRP_MJ_CREATE] = CreateCall;
	pDriverObject->MajorFunction[IRP_MJ_CLOSE] = CloseCall;
	pDriverObject->MajorFunction[IRP_MJ_DEVICE_CONTROL] = IoControl;
	pDriverObject->DriverUnload = UnloadDriver;

	DeviceObject->Flags |= DO_DIRECT_IO;
	DeviceObject->Flags &= ~DO_DEVICE_INITIALIZING;

	return STATUS_SUCCESS;
}