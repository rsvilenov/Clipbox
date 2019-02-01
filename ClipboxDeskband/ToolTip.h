#pragma once
#include <windows.h>
#include <uxtheme.h>

class ToolTip
{
private:
	HWND _tipHwnd;
	wchar_t _toolTipText[1024];
	TOOLINFOW _toolInfo;

public:
	bool Initialize(HWND parentHwnd, LPCWSTR text);
	bool Show();
	bool Hide();
};

