#include "NamedPipeClient.h"

#include <uxtheme.h>
#include <strsafe.h>

#define BUFFER_SIZE 1024
#define MAX_PIPE_NAME_LEN 100
#define MAX_CMD_LEN 100

HRESULT NamedPipeClient::GetPipeName(OUT LPWSTR pipeName)
{
	DWORD sessionId;

	if (!ProcessIdToSessionId(GetCurrentProcessId(), &sessionId))
	{
		return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, HRESULTS::HRESULT_ERROR_SESSION_ID);
	}

	return StringCchPrintf(pipeName, MAX_PIPE_NAME_LEN, L"%s.%d", PIPE_NAME_PREFIX, sessionId);
}

HRESULT NamedPipeClient::EnsureCommandIsALine(char* command)
{
	if (0 == strcmp(command + strlen(command) - 1, "\n"))
	{
		return S_OK;
	}

	return StringCchCatA(command, MAX_CMD_LEN, "\n");
}

HRESULT NamedPipeClient::WriteToPipe(HANDLE hPipe, const char* message)
{
	DWORD dwWritten;
	BOOL writeSuccess;

	// send the message
	writeSuccess = WriteFile(hPipe,
		message,
		(DWORD)strlen(message) + 1,   // = length of string + terminating '\0' !!!
		&dwWritten,
		NULL);

	if (!writeSuccess)
	{
		DWORD lastError = GetLastError();
		// async write operations cause WriteFile to return false,
		// but this is not an error
		if (lastError != ERROR_IO_PENDING)
		{
			return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, HRESULTS::HRESULT_WRITE_TO_PIPE_ERROR);
		}
	}

	return S_OK;
}

HRESULT NamedPipeClient::VerifyResponse(HANDLE hPipe)
{
	char readBuffer[BUFFER_SIZE];
	char* readBufferPart;
	DWORD numberOfBytesRead = 0;
	BOOL readSuccess = false;

	ZeroMemory(readBuffer, BUFFER_SIZE);

	// drain the pipe to the buffer

	// set the readBufferPart pointer to the beginning of readBuffer
	readBufferPart = readBuffer;
	do
	{
		// move the pointer
		readBufferPart = readBufferPart + numberOfBytesRead;

		readSuccess = ReadFile(hPipe, readBufferPart, BUFFER_SIZE, &numberOfBytesRead, NULL);
		if (!readSuccess)
		{
			DWORD lastError = GetLastError();
			// if the server has disconnected us, this most probably means that we have read all contents already.
			// Such is the default behavior of Clipbox.Infrastructure.NamedPipeServer.
			if (lastError != ERROR_IO_PENDING && lastError != ERROR_PIPE_NOT_CONNECTED)
			{
				return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, HRESULTS::HRESULT_READ_FROM_PIPE_ERROR);
			}
		}
	} while (numberOfBytesRead != 0);

	// check the response
	if (strcmp(readBuffer, PIPE_RESPONSE_OK) == 0)
	{
		return S_OK;
	}
	else
	{
		return MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, HRESULTS::HRESULT_WRONG_PIPE_SERVER_RESPONSE);
	}
}

HRESULT NamedPipeClient::SendToPipe(IN LPCWSTR pipeName, const char* message)
{
	HRESULT hr = S_OK;
	DWORD lastError = ERROR_SUCCESS;
	HANDLE hPipe;

	// connect to the pipe
	hPipe = CreateFile(pipeName,
		GENERIC_READ | GENERIC_WRITE,
		0,
		NULL,
		OPEN_EXISTING,
		0,
		NULL);

	if (hPipe != INVALID_HANDLE_VALUE)
	{
		hr = WriteToPipe(hPipe, message);
		if (SUCCEEDED(hr))
		{
			hr = VerifyResponse(hPipe);
		}
	}
	else
	{
		hr = E_HANDLE;
	}

	if (FAILED(hr))
	{
		lastError = GetLastError();
	}

	if (!CloseHandle(hPipe) && lastError != ERROR_SUCCESS)
	{
		// CloseHandle will overwrite the last error if it fails,
		// but we are more interested in the previous errors, if any.
		SetLastError(lastError);
	}

	return hr;
}

HRESULT NamedPipeClient::SendPipeMessage(const char* message)
{
	wchar_t pipeName[MAX_PIPE_NAME_LEN];
	char msgCopy[MAX_CMD_LEN];
	HRESULT hr;

	// copy the const wchar_t (LPCWSTR) to a new string...
	strcpy_s(msgCopy, message);

	// add a final '\n' to the string
	hr = EnsureCommandIsALine(msgCopy);

	if (SUCCEEDED(hr))
	{
		hr = GetPipeName(pipeName);
		if (SUCCEEDED(hr) &&
			!WaitNamedPipeW(pipeName, PIPE_WAIT_TIMEOUT_MS)) // check if the pipe exists...
		{
			hr = MAKE_HRESULT(SEVERITY_ERROR, FACILITY_WIN32, HRESULTS::HRESULT_WAIT_PIPE_ERROR);
		}
		else
		{
			hr = SendToPipe(pipeName, msgCopy);
		}
	}

	return hr;
}