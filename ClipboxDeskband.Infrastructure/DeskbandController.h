#pragma once
#include <Windows.h>

class DeskbandController
{

private:
	CLSID				_deskbandClsId;

public:
	DeskbandController(CLSID _deskbandClsId);

	HRESULT				TryRemoveDeskband(bool* removeSuccess);
	HRESULT             IsDeskbandShown(bool* isShown);
	HRESULT             LoadDeskband(bool* isDeskbandRegistered);
	HRESULT             ShowDeskband();
	HRESULT				NotiryDeskbandRegistrationChange();
};