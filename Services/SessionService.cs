﻿global using Lagerhotell.Services.State;
using Microsoft.JSInterop;

namespace Lagerhotell.Services;

public class SessionService
{
    private readonly IJSRuntime _jsRuntime;
    private AppState appState { get; set; }
    public SessionService(IJSRuntime jsRuntime, AppState _appState)
    {
        this._jsRuntime = jsRuntime;
        appState = _appState;
    }
    public async Task? AddJwtToLocalStorage(string jwt)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "jwtToken", jwt);
    }

    public async Task<string> GetJwtFromLocalStorage()
    {
        var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "jwtToken");
        return token ?? null;
    }

    /*public async Task<string> GetTokenPhoneNumber(string token)
    {
        DecodeJwt.DecodeJwtRequest request = new() { Token = token };
        string url = _baseUrl + "/decode-token";
        return "";
    } */

    public async Task RemoveItemAsync(string key)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
    }

    public async Task LogUserOut()
    {
        await RemoveItemAsync("jwtToken");
        appState.UserLoggedOut();
    }
}
