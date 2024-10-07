﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Lagerhotell.Services;

public class AuthStateProviderService : AuthenticationStateProvider
{
    private readonly SessionService _sessionService;
    private readonly HttpClient _tokenHttpClient;
    private readonly HttpClient _backofficeHttpClient;
    private readonly NavigationManager _navigationManager;
    private readonly UserService _userService;
    private readonly CompanyUserService _companyUserService;


    public AuthStateProviderService(SessionService sessionService, HttpClient tokenHttpClient, HttpClient backofficeHttpClient, NavigationManager navigationManager, UserService userService, CompanyUserService companyUserService)
    {
        _tokenHttpClient = tokenHttpClient;
        _backofficeHttpClient = backofficeHttpClient;
        _sessionService = sessionService;
        _navigationManager = navigationManager;
        _userService = userService;
        _companyUserService = companyUserService;
    }

    private IEnumerable<Claim> Parse(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer
            .Deserialize<Dictionary<string, object>>(jsonBytes);

        IEnumerable<Claim> claims = keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()));

        return claims;
    }

    /*private async Task<string> ExtractUserPhoneNumber(string jwt)
    {
        var claims = Parse(jwt);
        var phoneNumber = claims.FirstOrDefault(c => c.Type == ClaimTypes.MobilePhone)?.Value;
        return phoneNumber;
    }*/

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        string? accessToken = await _sessionService.GetJwtFromLocalStorage();

        var identity = new ClaimsIdentity();
        _tokenHttpClient.DefaultRequestHeaders.Authorization = null;
        _backofficeHttpClient.DefaultRequestHeaders.Authorization = null;

        if (!string.IsNullOrEmpty(accessToken))
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(accessToken);
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    await _sessionService.RemoveItemAsync("jwtToken");
                    identity = new ClaimsIdentity();
                    _navigationManager.NavigateTo("/logg-inn");
                    _navigationManager.NavigateTo(_navigationManager.Uri, true);
                    Console.WriteLine("Token expired");
                }
                identity = new ClaimsIdentity(Parse(accessToken), "jwt");
                accessToken = accessToken.Replace("\"", "").Replace("\"", "");

                _tokenHttpClient
                    .DefaultRequestHeaders
                    .Authorization = new AuthenticationHeaderValue("jwtToken", accessToken);

                _backofficeHttpClient
                    .DefaultRequestHeaders
                    .Authorization = new AuthenticationHeaderValue("jwtToken", accessToken);
            }
            catch
            {
                await _sessionService.RemoveItemAsync("jwtToken");
                identity = new ClaimsIdentity();
            }
        }

        var user = new ClaimsPrincipal(identity);
        var state = new AuthenticationState(user);

        NotifyAuthenticationStateChanged(Task.FromResult(state));

        return state;
    }

    public async Task<UserAndCompanyUser> GetUser()
    {
        User? user = new();
        CompanyUser? companyUser = new();
        var token = await _sessionService.GetJwtFromLocalStorage();
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var auth0Id = jwtToken.Claims.First(claim => claim.Type == "sub").Value;
        try
        {
            user = await _userService.GetUserByAuth0Id(auth0Id);
        } catch (HttpRequestException e)
        {
           companyUser = await _companyUserService.GetCompanyUserByAuth0Id(auth0Id);
        }

        return new UserAndCompanyUser(user, companyUser);
    }

    /*public async Task<string> GetCurrentUserPhoneNumber()
    {
        string token = await _sessionService.GetJwtFromLocalStorage();
        string? phoneNumber = await ExtractUserPhoneNumber(token);
        if (phoneNumber == null)
        {
            throw new KeyNotFoundException("Could not extract phone number from token");
        }
        return phoneNumber;
    }*/
}