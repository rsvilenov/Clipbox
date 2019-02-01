#include "ToolTip.h"
#include <CommCtrl.h>

bool ToolTip::Initialize(HWND parentHwnd, LPCWSTR text)
{
	lstrcpy(_toolTipText, text);

	// Create a tooltip.
	_tipHwnd = CreateWindowEx(WS_EX_TOPMOST, TOOLTIPS_CLASS, NULL,
		WS_POPUP | TTS_NOPREFIX | TTS_ALWAYSTIP,
		CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT,
		NULL, NULL, NULL, NULL);
	if (_tipHwnd)
	{
		_toolInfo = { 0 };
		_toolInfo.cbSize = sizeof(TOOLINFOW);
		_toolInfo.uFlags = TTF_TRACK | TTF_CENTERTIP;
		_toolInfo.hwnd = parentHwnd;
		_toolInfo.hinst = NULL;
		_toolInfo.lpszText = _toolTipText;

		SendMessage(_tipHwnd, TTM_ADDTOOL, 0, (LPARAM)(LPTOOLINFO)&_toolInfo);
		return true;
	}

	return false;
}

bool ToolTip::Show()
{
	POINT p;
	GetCursorPos(&p);

	SendMessage(_tipHwnd, TTM_TRACKPOSITION, 0, (LPARAM)(DWORD)MAKELONG(p.x, p.y));
	SendMessage(_tipHwnd, TTM_TRACKACTIVATE, true, (LPARAM)(LPTOOLINFO)&_toolInfo);

	return true;
}

bool ToolTip::Hide()
{
	SendMessage(_tipHwnd, TTM_TRACKACTIVATE, false, (LPARAM)(LPTOOLINFO)&_toolInfo);

	return true;
}
