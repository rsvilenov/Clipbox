#pragma once
#include <Windows.h>

class Logger
{

private:

	wchar_t _logFilePath[MAX_PATH];
	bool _isFirstWrite;
	HANDLE _mutex = NULL;

	HRESULT LockAndWriteToLogFile(char *data);

	HRESULT WriteToLogFile(char *data);

	errno_t DateTimeNow(char* dateTimeStr);

public:
	Logger(wchar_t *logFilePath);

	HRESULT LogText(char* text);

	HRESULT LogHR(HRESULT hr);

	HRESULT LogLastWin32Error();

};