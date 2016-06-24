namespace Elpis.Util
{
    public enum ErrorCodes
    {
        // _ prefix means not a specific definition
        UnknownError = -2000,
        SystemError = -1999,
        Success = -1,
        Internal = 0,
        MaintenanceMode = 1,
        UrlParamMissingMethod = 2,
        UrlParamMissingAuthToken = 3,
        UrlParamMissingPartnerId = 4,
        UrlParamMissingUserId = 5,
        SecureProtocolRequired = 6,
        CertificateRequired = 7,
        ParameterTypeMismatch = 8,
        ParameterMissing = 9,
        ParameterValueInvalid = 10,
        ApiVersionNotSupported = 11,
        InvalidCountry = 12,
        InsufficientConnectivity = 13,
        InvalidMethod = 14,
        SecureRequired = 15,
        ReadOnlyMode = 1000,
        InvalidAuthToken = 1001,
        InvalidPartnerLogin = 1002,
        ListenerNotAuthorized = 1003,
        UserNotAuthorized = 1004,
        EndOfPlaylist = 1005,
        StationDoesNotExist = 1006,
        ComplimentaryPeriodAlreadyInUse = 1007,
        CallNotAllowed = 1008,
        DeviceNotFound = 1009,
        PartnerNotAuthorized = 1010,
        InvalidUsername = 1011,
        InvalidPassword = 1012,
        UsernameAlreadyExists = 1013,
        DeviceAlreadyAssociatedToAccount = 1014,
        UpgradeDeviceModelInvalid = 1015,
        ExplicitPinIncorrect = 1018,
        ExplicitPinMalformed = 1020,
        DeviceModelInvalid = 1023,
        ZipCodeInvalid = 1024,
        BirthYearInvalid = 1025,
        BirthYearTooYoung = 1026,
        InvalidCountryCode = 1027,
        InvalidGender = 1027,
        DeviceDisabled = 1034,
        DailyTrialLimitReached = 1035,
        InvalidSponsor = 1036,
        UserAlreadyUsedTrial = 1037,

        NoAudioUrls = 5000,

        //Elpis Specific
        ErrorRpc = 6000,
        ConfigLoadError = 6001,
        LogSetupError = 6002,
        EngineInitError = 6003,
        StreamError = 6004,

        //LastFM Errors
        ErrorGettingSession = 7000,
        ErrorGettingToken = 7001
    }

    public class Errors
    {
        private static readonly string UPDATE_REQUIRED =
            "Elpis requires an update.\r\nPlease check http://adamhaile.net for details.";

        public static string GetErrorMessage(ErrorCodes faultCode, string msg = "Unknown Error")
        {
            switch (faultCode)
            {
                case ErrorCodes.UnknownError:
                    return "An unknown error occured.";
                case ErrorCodes.SystemError:
                    return "An internal client error occured.";
                case ErrorCodes.Success:
                    return "Operation completed successfully.";
                case ErrorCodes.Internal:
                    return "An internal error occured.";
                case ErrorCodes.MaintenanceMode:
                    return "Pandora is currently conducting maintenance. Please try again later.";
                case ErrorCodes.UrlParamMissingMethod:
                    return UPDATE_REQUIRED;
                case ErrorCodes.UrlParamMissingAuthToken:
                    return UPDATE_REQUIRED;
                case ErrorCodes.UrlParamMissingPartnerId:
                    return UPDATE_REQUIRED;
                case ErrorCodes.UrlParamMissingUserId:
                    return UPDATE_REQUIRED;
                case ErrorCodes.SecureProtocolRequired:
                    return UPDATE_REQUIRED;
                case ErrorCodes.CertificateRequired:
                    return UPDATE_REQUIRED;
                case ErrorCodes.ParameterTypeMismatch:
                    return UPDATE_REQUIRED;
                case ErrorCodes.ParameterMissing:
                    return UPDATE_REQUIRED;
                case ErrorCodes.ParameterValueInvalid:
                    return UPDATE_REQUIRED;
                case ErrorCodes.ApiVersionNotSupported:
                    return UPDATE_REQUIRED;
                case ErrorCodes.InvalidCountry:
                    return "The country you are connecting from is not allowed to access Pandora.";
                case ErrorCodes.InsufficientConnectivity:
                    return
                        "INSUFFICIENT_CONNECTIVITY. Possibly invalid sync time. Try logging in again, and check for client updates.";
                case ErrorCodes.InvalidMethod:
                    return "Incorrect HTTP/S method used for last RPC. " + UPDATE_REQUIRED;
                case ErrorCodes.SecureRequired:
                    return "SSL required for last RPC. " + UPDATE_REQUIRED;
                case ErrorCodes.ReadOnlyMode:
                    return "Pandora is currently conducting maintenance. Please try again later.";
                case ErrorCodes.InvalidAuthToken:
                    return "Auth token is invalid/expired.";
                case ErrorCodes.InvalidPartnerLogin:
                    return "Partner or user login is invalid.";
                case ErrorCodes.ListenerNotAuthorized:
                    return "Your subscription has lapsed. Please visit www.pandora.com to confirm account status.";
                case ErrorCodes.UserNotAuthorized:
                    return "This user may not perform that action.";
                case ErrorCodes.EndOfPlaylist:
                    return "End of playlist detected. Skip limit exceeded.";
                case ErrorCodes.StationDoesNotExist:
                    return "Station does not exist.";
                case ErrorCodes.ComplimentaryPeriodAlreadyInUse:
                    return "Pandora One Trial is currently active.";
                case ErrorCodes.CallNotAllowed:
                    return "Permission denied to use this call.";
                case ErrorCodes.DeviceNotFound:
                    return "Device not found.";
                case ErrorCodes.PartnerNotAuthorized:
                    return "partnerLogin is invalid. " + UPDATE_REQUIRED;
                case ErrorCodes.InvalidUsername:
                    return "Specified username is not valid.";
                case ErrorCodes.InvalidPassword:
                    return "Specified password is not valid.";
                case ErrorCodes.UsernameAlreadyExists:
                    return "This username is already in use.";
                case ErrorCodes.DeviceAlreadyAssociatedToAccount:
                    return "Device already associated.";
                case ErrorCodes.UpgradeDeviceModelInvalid:
                    return "This client is out of date.";
                case ErrorCodes.ExplicitPinIncorrect:
                    return "Explicit PIN is incorrect.";
                case ErrorCodes.ExplicitPinMalformed:
                    return "Explicit PIN is invalid.";
                case ErrorCodes.DeviceModelInvalid:
                    return "Device model is not valid. Client out of date.";
                case ErrorCodes.ZipCodeInvalid:
                    return "ZIP code is not valid.";
                case ErrorCodes.BirthYearInvalid:
                    return "Birth year is not valid.";
                case ErrorCodes.BirthYearTooYoung:
                    return "You must be 13 or older to use the Pandora service.";
                //case ErrorCodes.INVALID_COUNTRY_CODE: return "Country code invalid.";
                case ErrorCodes.InvalidGender:
                    return "Invalid gender: 'male' or 'female' expected.";
                case ErrorCodes.DeviceDisabled:
                    return "Device disabled.";
                case ErrorCodes.DailyTrialLimitReached:
                    return "You may not activate any more trials.";
                case ErrorCodes.InvalidSponsor:
                    return "Invalid sponsor.";
                case ErrorCodes.UserAlreadyUsedTrial:
                    return "You have already used your Pandora One trial.";

                case ErrorCodes.NoAudioUrls:
                    return "No Audio URLs returned for this track.";

                case ErrorCodes.ErrorRpc:
                    return
                        "Error communicating with the server. \r\nTry again, check your connection or try restarting.";
                case ErrorCodes.ConfigLoadError:
                    return
                        @"Error loading Elpis configuration. Try navigating to %AppData%\Elpis\ and deleting ""elpis.config""";
                case ErrorCodes.LogSetupError:
                    return "Error setting up logging.";
                case ErrorCodes.EngineInitError:
                    return "Error initializing the player engine, Elpis must close. Try restarting the application.";
                case ErrorCodes.StreamError:
                    return "Failed to load song more than once.\r\nCheck connection and try again.";

                case ErrorCodes.ErrorGettingSession:
                    return "Error retrieving Last.FM Session.";
                case ErrorCodes.ErrorGettingToken:
                    return "Error retrieving Last.FM Auth Token.";

                default:
                    return msg;
            }
        }

        public static bool IsHardFail(ErrorCodes faultCode)
        {
            switch (faultCode)
            {
                case ErrorCodes.UrlParamMissingMethod:
                case ErrorCodes.UrlParamMissingAuthToken:
                case ErrorCodes.UrlParamMissingPartnerId:
                case ErrorCodes.UrlParamMissingUserId:
                case ErrorCodes.SecureProtocolRequired:
                case ErrorCodes.CertificateRequired:
                case ErrorCodes.ParameterTypeMismatch:
                case ErrorCodes.ParameterMissing:
                case ErrorCodes.ParameterValueInvalid:
                case ErrorCodes.ApiVersionNotSupported:
                case ErrorCodes.PartnerNotAuthorized:
                case ErrorCodes.ConfigLoadError:
                case ErrorCodes.LogSetupError:
                case ErrorCodes.EngineInitError:
                    return true;
                case ErrorCodes.UnknownError:
                    break;
                case ErrorCodes.SystemError:
                    break;
                case ErrorCodes.Success:
                    break;
                case ErrorCodes.Internal:
                    break;
                case ErrorCodes.MaintenanceMode:
                    break;
                case ErrorCodes.InvalidCountry:
                    break;
                case ErrorCodes.InsufficientConnectivity:
                    break;
                case ErrorCodes.InvalidMethod:
                    break;
                case ErrorCodes.SecureRequired:
                    break;
                case ErrorCodes.ReadOnlyMode:
                    break;
                case ErrorCodes.InvalidAuthToken:
                    break;
                case ErrorCodes.InvalidPartnerLogin:
                    break;
                case ErrorCodes.ListenerNotAuthorized:
                    break;
                case ErrorCodes.UserNotAuthorized:
                    break;
                case ErrorCodes.EndOfPlaylist:
                    break;
                case ErrorCodes.StationDoesNotExist:
                    break;
                case ErrorCodes.ComplimentaryPeriodAlreadyInUse:
                    break;
                case ErrorCodes.CallNotAllowed:
                    break;
                case ErrorCodes.DeviceNotFound:
                    break;
                case ErrorCodes.InvalidUsername:
                    break;
                case ErrorCodes.InvalidPassword:
                    break;
                case ErrorCodes.UsernameAlreadyExists:
                    break;
                case ErrorCodes.DeviceAlreadyAssociatedToAccount:
                    break;
                case ErrorCodes.UpgradeDeviceModelInvalid:
                    break;
                case ErrorCodes.ExplicitPinIncorrect:
                    break;
                case ErrorCodes.ExplicitPinMalformed:
                    break;
                case ErrorCodes.DeviceModelInvalid:
                    break;
                case ErrorCodes.ZipCodeInvalid:
                    break;
                case ErrorCodes.BirthYearInvalid:
                    break;
                case ErrorCodes.BirthYearTooYoung:
                    break;
                case ErrorCodes.InvalidCountryCode:
                    break;
                case ErrorCodes.DeviceDisabled:
                    break;
                case ErrorCodes.DailyTrialLimitReached:
                    break;
                case ErrorCodes.InvalidSponsor:
                    break;
                case ErrorCodes.UserAlreadyUsedTrial:
                    break;
                case ErrorCodes.NoAudioUrls:
                    break;
                case ErrorCodes.ErrorRpc:
                    break;
                case ErrorCodes.StreamError:
                    break;
                case ErrorCodes.ErrorGettingSession:
                    break;
                case ErrorCodes.ErrorGettingToken:
                    break;
                default:
                    return false;
            }
            return false;
        }
    }
}