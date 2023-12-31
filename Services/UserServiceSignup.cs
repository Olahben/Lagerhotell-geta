﻿using Lagerhotell.Pages;
using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Lagerhotell.Services.UserService
{
    public class UserServiceSignup
    {
        // private static readonly HttpClient client = new();
        // public Signup signup = new();
        public string CustomError;
        public bool UserRegistered;
        public async Task AddUser(string firstName, string lastName, string phoneNumber, string birthDate, string password, HttpClient client)
        {
            var request = LagerhotellAPI.Models.AddUserRequest.AddUserRequestFunc(firstName, lastName, phoneNumber, birthDate, password);
            string url = "https://localhost:7272/users/adduser";
            string jsonData = JsonSerializer.Serialize(request);
            StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, content);
            // Handle the ID that is returned
        }

        public async Task? RedirectToLogin(NavigationManager navigationManager)
        {
            // Redirects to login
            navigationManager.NavigateTo("/success-login");
        }
        public async Task? PhoneNumberExistence(string phoneNumber, HttpClient client)
        {
            string url = "https://localhost:7272/users/is-phone-number-registered-registration";
            var request = new LagerhotellAPI.Models.CheckPhoneNumber.CheckPhoneNumberRequest { PhoneNumber = phoneNumber };
            string jsonData = JsonSerializer.Serialize(request);
            StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, content);
            Console.WriteLine("response" + response.ReasonPhrase);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Phone number registered");
                CustomError = "Mobilnummeret har allerede blitt registrert";
                UserRegistered = true;
            }
            else
            {
                CustomError = "";
                UserRegistered = false;
            }
        }
        public async Task<string> SignupUser(NavigationManager navigationManager, Signup.AccountFormValues accountFormValues, HttpClient client)
        {
            await PhoneNumberExistence(accountFormValues.PhoneNumber, client);
            if (!UserRegistered)
            {
                await RedirectToLogin(navigationManager);
                await AddUser(accountFormValues.FirstName, accountFormValues.FirstName, accountFormValues.PhoneNumber, accountFormValues.BirthDate, accountFormValues.Password, client);
            }
            return CustomError;

        }
        public class ContainsNumberAttribute : ValidationAttribute
        {
            // Overrides the existing method IsValid in the ValidationAttribute class
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                string password = value as string;
                // Checks if the password contains a number
                if (password.Any(char.IsDigit))
                {
                    return ValidationResult.Success;
                }
                else
                {
                    return new ValidationResult("Passordet ditt må ha minst ett tall");
                }
            }
        }

        public class ContainsOnlyNumbersAttribute : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                string phoneNumber = value as string;
                if (int.TryParse(phoneNumber, out int result))
                {
                    return ValidationResult.Success;
                }
                else
                {
                    return new ValidationResult("Telefonnummeret ditt kan bare inneholde tall");
                }
            }
        }
        // Inherits from the ValidationAttribute class
        public class IsOverLegalAgeAttribute : ValidationAttribute
        {
            // Overrides the existing method IsValid in the ValidationAttribute class
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                string birthDateStr = value as string;
                // format is with hyphens
                DateTime birthDate = DateTime.ParseExact(birthDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                int age = DateTime.Now.Year - birthDate.Year;
                // Checks exactly if the person is over 18
                if (DateTime.Now.Month < birthDate.Month || (DateTime.Now.Month == birthDate.Month && DateTime.Now.Day < birthDate.Day))
                    age--;

                if (age >= 18)
                {
                    return ValidationResult.Success;
                }
                else
                {
                    return new ValidationResult("Du må være over 18 år");
                }

            }
        }
    }
}
