#include "DeskbandController.h"

#include <shlobj.h> 

#include "../ClipboxDeskband.Infrastructure/ClipboxProductConstants.h"
#include "../ClipboxDeskband.Infrastructure/Logger.h"
#pragma comment(lib, "ClipboxDeskband.Infrastructure.lib")

DeskbandController::DeskbandController(CLSID deskbandClsId)
{
	_deskbandClsId = deskbandClsId;
}


HRESULT DeskbandController::NotiryDeskbandRegistrationChange()
{
    HRESULT hr = CoInitialize(nullptr);
    ITrayDeskBand* pTrayDeskBand = NULL;
    hr = CoCreateInstance(CLSID_TrayDeskBand, NULL, CLSCTX_ALL, IID_PPV_ARGS(&pTrayDeskBand));

    if (SUCCEEDED(hr))
    {
        hr = pTrayDeskBand->DeskBandRegistrationChanged();
        pTrayDeskBand->Release();
    }

    CoUninitialize();

    return hr;
}

HRESULT DeskbandController::IsDeskbandShown(bool* isShown)
{
    *isShown = false;

    HRESULT hr = CoInitialize(nullptr);
    ITrayDeskBand* pTrayDeskBand = NULL;
    hr = CoCreateInstance(CLSID_TrayDeskBand, NULL, CLSCTX_ALL, IID_PPV_ARGS(&pTrayDeskBand));

    if (SUCCEEDED(hr))
    {
        hr = pTrayDeskBand->IsDeskBandShown(PRODCONST_CLSID_ClipboxDeskBand);
        if (SUCCEEDED(hr) && (S_OK == hr))
        {
            *isShown = true;
        }

        pTrayDeskBand->Release();
    }

    CoUninitialize();

    return hr;
}

HRESULT DeskbandController::ShowDeskband()
{
    HRESULT hr = CoInitialize(nullptr);
    ITrayDeskBand* pTrayDeskBand = NULL;
    hr = CoCreateInstance(CLSID_TrayDeskBand, NULL, CLSCTX_ALL, IID_PPV_ARGS(&pTrayDeskBand));

    if (SUCCEEDED(hr))
    {
        hr = pTrayDeskBand->DeskBandRegistrationChanged();
        if (SUCCEEDED(hr))
        {
            hr = pTrayDeskBand->ShowDeskBand(PRODCONST_CLSID_ClipboxDeskBand);
            if (SUCCEEDED(hr))
            {
                hr = pTrayDeskBand->DeskBandRegistrationChanged();
            }
        }
        pTrayDeskBand->Release();
    }

    CoUninitialize();

    return hr;
}


HRESULT DeskbandController::TryRemoveDeskband(bool* removeSuccess)
{
    *removeSuccess = false;

    HRESULT hr = CoInitialize(NULL);
    if (FAILED(hr))
    {
        return hr;
    }

    IBandSite* pBandSite;
    hr = CoCreateInstance(CLSID_TrayBandSiteService, NULL, CLSCTX_SERVER, IID_IBandSite, (LPVOID*)&pBandSite);

    if (SUCCEEDED(hr))
    {
        IDeskBand* pBand = 0;
        DWORD bandID = 0;

        int i = 0;
        while (SUCCEEDED(pBandSite->EnumBands(i, &bandID)))
        {
            i++;

            hr = pBandSite->GetBandObject(bandID, IID_IDeskBand, (void**)&pBand);
            if (FAILED(hr))
            {
                continue;
            }

            IPersist* pPersist = 0;
            hr = pBand->QueryInterface(IID_IPersist, (void**)&pPersist);
            if (SUCCEEDED(hr))
            {
                CLSID clsid = CLSID_NULL;
                hr = pPersist->GetClassID(&clsid);
                if (SUCCEEDED(hr))
                {
                    if (clsid == PRODCONST_CLSID_ClipboxDeskBand)
                    {
                        //HRESULT hr = pBandSite->SetBandState(bandID, BSSF_VISIBLE, UINT_MAX);
                        hr = pBandSite->RemoveBand(bandID);
                        *removeSuccess = SUCCEEDED(hr);
                        break;
                    }
                }
                pPersist->Release();

            }

            pBand->Release();
        }

        pBandSite->Release();
    }


    CoUninitialize();

    return hr;
}

// REGDB_E_CLASSNOTREG is about the band if isDeskbandRegistered is false,
// Otherwise - it means that the Windows Explorer is not fully loaded yet.
HRESULT DeskbandController::LoadDeskband(bool* isDeskbandRegistered)
{
    *isDeskbandRegistered = false;
    HRESULT hr = S_OK;

    hr = CoInitialize(nullptr);
    if (FAILED(hr))
    {
        return hr;
    }

    IDeskBand2* pDeskbandInterface = NULL;

    // CLSID_ClipboxDeskBand is defined in ClipboxProductConstants.h (ClipboxDeskband.Infrastructure)
    hr = CoCreateInstance(PRODCONST_CLSID_ClipboxDeskBand, NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&pDeskbandInterface));
    if (SUCCEEDED(hr))
    {
        *isDeskbandRegistered = SUCCEEDED(hr);
        IBandSite* pBandSite = NULL;

        hr = CoCreateInstance(CLSID_TrayBandSiteService, NULL, CLSCTX_ALL, IID_PPV_ARGS(&pBandSite));
        if (SUCCEEDED(hr))
        {
            hr = pBandSite->AddBand(pDeskbandInterface);
            pBandSite->Release();
        }

        pDeskbandInterface->Release();
    }

    CoUninitialize();

    return hr;
}