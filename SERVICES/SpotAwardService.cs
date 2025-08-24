using System;
using System.Collections.Generic;
using SpotAward.Models;
using SpotAward.BUSINESSLOGIC;
using Microsoft.Extensions.Configuration;

namespace SpotAward.SERVICES
{
    public class SpotAwardService
    {
        private readonly SpotAwardBusinessLogic _businessLogic;
        private readonly string _connectionString;

        // Constructor for ASP.NET Core DI
        public SpotAwardService(IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration), "Configuration cannot be null");
            }
            
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                throw new InvalidOperationException("DefaultConnection string is not found in configuration");
                
            _businessLogic = new SpotAwardBusinessLogic(_connectionString);
        }

        // Overloaded constructor (useful for unit tests or manual usage)
        public SpotAwardService(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty");
            }
            
            _connectionString = connectionString;
            _businessLogic = new SpotAwardBusinessLogic(_connectionString);
        }

        public ValidationResult ValidateRequest(int initiatorMEmpID)
        {
            // For demo purposes, we'll just check if the user is authorized
            // You can add more validation logic here as needed
            bool isAuthorized = IsUserAuthorized(initiatorMEmpID);
            
            return new ValidationResult
            {
                IsAuthorized = isAuthorized,
                IsNewRequestAllowed = isAuthorized, // Assuming if authorized, they can make a new request
                IsEligibleNominee = true, // Default to true since we're not validating nominee here
                Message = isAuthorized ? "Validation successful" : "User is not authorized"
            };
        }

        //public ValidationResult ValidateRequest(int initiatorMEmpID, int nomineeMEmpID, int initiatorMGID)
        //{
        //    return _businessLogic.ValidateSpotAwardRequest(initiatorMEmpID, nomineeMEmpID, initiatorMGID);
        //}

        public int CreateSpotAwardRequest(int initiatorMEmpID, int nomineeMEmpID, string justification, int initiatorMGID, bool isGHTHInitiated = false)
        {
            var request = new SpotAwardRequest
            {
                SAID = 0,
                InitiatorMEmpID = initiatorMEmpID,
                NomineeMEmpID = nomineeMEmpID,
                Justification = justification,
                IsGHTHInitiated = isGHTHInitiated,
                IsDelivered = false,
                InitiatorMGID = initiatorMGID,
                CreatedDate = DateTime.Now
            };

            return _businessLogic.SubmitSpotAwardRequest(request);
        }

        public QuotaDetails GetQuotaInformation(int depMGID)
        {
            return _businessLogic.GetQuotaDetails(depMGID);
        }

        public List<EmployeeDetails> GetEmployeeList(string employeeName = null)
        {
            return _businessLogic.GetEmployeesForNomination(employeeName);
        }

        public bool CheckNomineeEligibility(int nomineeMEmpID, int initiatorMEmpID)
        {
            // This method checks if a nominee is eligible based on level criteria
            // It uses the IsNomineeEmpRegularAndUnderEqualLevel method from the business logic layer
            // which verifies if the nominee is a regular employee and at or below the initiator's level
            return _businessLogic.CheckNomineeEligibility(nomineeMEmpID, initiatorMEmpID);
        }

        public SpotAwardRequest GetRequestDetails(int sAID)
        {
            return _businessLogic.GetSpotAwardDetails(sAID);
        }

        //public List<SpotAwardRequest> GetDepartmentHistory(int depMGID)
        //{
        //    return _businessLogic.GetNominationHistory(depMGID);
        //}

        public bool IsUserAuthorized(int initiatorMEmpID)
        {
            return _businessLogic.IsUserAuthorized(initiatorMEmpID);
        }
    }
}
