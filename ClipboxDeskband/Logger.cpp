#include "Logger.h"
#include "comdef.h"
#include <time.h>
#include <strsafe.h>


Logger::Logger(wchar_t *logFilePath)
{
	lstrcpy(_logFilePath, logFilePath);
	_isFirstWrite = true;
}


HRESULT Logger::LogText(char* text)
{
	char dateTimeStr[256];
	char completeMessage[1024];
	HRESULT hr;

	if (DateTimeNow(dateTimeStr) != 0)
	{
		hr = E_FAIL;
	}
	else
	{
		hr = StringCchPrintfA(completeMessage, ARRAYSIZE(completeMessage), "%s\nMsg:\n%s\n", dateTimeStr, text);
		if (SUCCEEDED(hr))
		{
			hr = LockAndWriteToLogFile(completeMessage);
		}
	}

	return hr;
}


HRESULT Logger::LogHR(HRESULT hResult)
{
	_com_error err(hResult);
	LPCTSTR errMsg = err.ErrorMessage();

	char dateTimeStr[256];
	if (DateTimeNow(dateTimeStr) != 0)
	{
		return E_FAIL;
	}

	int sizeRequired = WideCharToMultiByte(CP_UTF8, 0, errMsg, -1,
		NULL, 0, NULL, NULL);
	char* strErrMessage = (char*)malloc(sizeRequired);
	if (!strErrMessage)
	{
		return E_OUTOFMEMORY;
	}
	WideCharToMultiByte(CP_UTF8, 0, errMsg, -1,
		strErrMessage, sizeRequired, NULL, NULL);

	char completeMessage[1024];
	HRESULT hr = StringCchPrintfA(completeMessage, ARRAYSIZE(completeMessage), "%s\nHR:\n%s\n", dateTimeStr, strErrMessage);
	if (SUCCEEDED(hr))
	{
		hr = LockAndWriteToLogFile(completeMessage);
	}

	free(strErrMessage);

	if (GetLastError() != ERROR_SUCCESS)
	{
		LogLastWin32Error();
	}

	return hr;
}

HRESULT Logger::LogLastWin32Error()
{
	char buf[256];
	FormatMessageA(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
		NULL, GetLastError(), MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
		buf, (sizeof(buf) / sizeof(char)), NULL);

	char dateTimeStr[256];
	if (DateTimeNow(dateTimeStr) != 0)
	{
		return E_FAIL;
	}

	char completeMessage[1024];
	HRESULT hr = StringCchPrintfA(completeMessage, ARRAYSIZE(completeMessage), "%s\nWin32:\n%s\n", dateTimeStr, buf);
	if (SUCCEEDED(hr))
	{
		hr = LockAndWriteToLogFile(completeMessage);
	}

	return hr;
}

errno_t Logger::DateTimeNow(char* dateTimeStr)
{
	// https://stackoverflow.com/questions/997946/how-to-get-current-time-and-date-in-c
	time_t     now = time(0);
	struct tm  tstruct;
	errno_t error = localtime_s(&tstruct, &now);
	if (error == 0)
	{
		strftime(dateTimeStr, 21, "%Y-%m-%d %H:%S:%M", &tstruct);
	}

	return error;
}

// we are in a STA thread so locking is not needed,
// but I'll leave it for now in order to move it
// some day to a library class for future use...
HRESULT Logger::LockAndWriteToLogFile(char *data)
{
	DWORD dwWaitResult;
	HRESULT hr = S_OK;

	if (!_mutex)
	{
		_mutex = CreateMutex(
			NULL,              // default security attributes
			FALSE,             // initially not owned
			NULL);             // unnamed mutex
	}

	if (!_mutex)
	{
		return E_FAIL;
	}

	dwWaitResult = WaitForSingleObject(
		_mutex,    // handle to mutex
		INFINITE);  // no time-out interval

	if (dwWaitResult == WAIT_OBJECT_0) // The thread got ownership of the mutex
	{
		hr = WriteToLogFile(data);

		if (!ReleaseMutex(_mutex))
		{
			hr = E_FAIL;
		}
	}

	return hr;
}

HRESULT Logger::WriteToLogFile(char *data)
{
	// https://stackoverflow.com/questions/43529314/how-to-append-data-on-a-file-in-win32

	HANDLE hFile;
	DWORD dwBytesToWrite = (DWORD)strlen(data);
	DWORD dwBytesWritten;
	BOOL bErrorFlag = FALSE;
	HRESULT hr = S_OK;

	DWORD dwDesiredAccess = _isFirstWrite ?
		FILE_WRITE_DATA :
		FILE_APPEND_DATA; // open for appending
	
	hFile = CreateFileW(_logFilePath,  // name of the write
		dwDesiredAccess,
		FILE_SHARE_READ,           // share for reading only
		NULL,                      // default security
		OPEN_ALWAYS,               // open existing file or create new file 
		FILE_ATTRIBUTE_NORMAL,     // normal file
		NULL);                     // no attr. template

	// CreateFileW is among the few apis that return
	// -1 instead of NULL on failure (INVALID_HANDLE_VALUE = -1)
	if (hFile == INVALID_HANDLE_VALUE)
	{
		return E_HANDLE;
	}

	if (_isFirstWrite)
	{
		// truncate the file
		SetEndOfFile(hFile);
	}

	_isFirstWrite = false;

	while (dwBytesToWrite > 0)
	{
		bErrorFlag = WriteFile(
			hFile,              // open file handle
			data,               // start of data to write
			dwBytesToWrite,     // number of bytes to write
			&dwBytesWritten,    // number of bytes that were written
			NULL);              // no overlapped structure

		if (!bErrorFlag)
		{
			hr = E_FAIL;
			break;
		}

		data += dwBytesWritten;
		dwBytesToWrite -= dwBytesWritten;
	}

	CloseHandle(hFile);

	return hr;
}
