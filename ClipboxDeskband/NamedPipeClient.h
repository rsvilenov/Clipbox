#pragma once
#include <Windows.h>

class NamedPipeClient
{

private:

	LPCWSTR PIPE_NAME_PREFIX = L"\\\\.\\pipe\\ClipboxShExPipe";
	const char* PIPE_RESPONSE_OK = "OK\r\n";
	const int PIPE_WAIT_TIMEOUT_MS = 1000;

	HRESULT GetPipeName(OUT LPWSTR pipeName);
	HRESULT WriteToPipe(HANDLE hPipe, const char* message);
	HRESULT VerifyResponse(HANDLE hPipe);
	HRESULT EnsureCommandIsALine(char* command);
	HRESULT SendToPipe(IN LPCWSTR pipeName, const char* message);

public:
	HRESULT SendPipeMessage(const char* message);
	enum HRESULTS
	{
		HRESULT_ERROR_SESSION_ID = 10001,
		HRESULT_WRONG_PIPE_SERVER_RESPONSE = 10002,
		HRESULT_WAIT_PIPE_ERROR = 10003,
		HRESULT_WRITE_TO_PIPE_ERROR = 10004,
		HRESULT_READ_FROM_PIPE_ERROR = 10005
	};
};

