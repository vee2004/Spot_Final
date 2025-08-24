using System;
using System.Collections.Generic;
using SpotAward.Models;
using SpotAward.DATAACCESS;
#nullable enable

namespace SpotAward.BUSINESSLOGIC
{
    public class SpotAwardBusinessLogic
    {
        private readonly SpotAwardDataAccess _dataAccess;

        public SpotAwardBusinessLogic(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty");
            }
            _dataAccess = new SpotAwardDataAccess(connectionString);
        }

        public ValidationResult ValidateSpotAwardRequest(int initiatorMEmpID, int nomineeMEmpID, int initiatorMGID)
        {
            ValidationResult result = new ValidationResult();

            try
            {
                // Step 1: Check if user is authorized
                result.IsAuthorized = _dataAccess.IsRMPHGHTH(initiatorMEmpID);
                if (!result.IsAuthorized)
                {
                    result.Message = "User is not authorized to initiate spot award requests.";
                    return result;
                }

                // Step 2: Check quota availability
                var quotaValidation = _dataAccess.IsNewRequestAllowed(initiatorMEmpID, initiatorMGID);
                result.IsNewRequestAllowed = quotaValidation.IsNewRequestAllowed;
                result.DepMGID = quotaValidation.DepMGID;
                
                if (!result.IsNewRequestAllowed)
                {
                    result.Message = quotaValidation.Message;
                    return result;
                }

                // Step 3: Check nominee eligibility
                result.IsEligibleNominee = _dataAccess.IsNomineeEmpRegularAndUnderEqualLevel(nomineeMEmpID, initiatorMEmpID);
                if (!result.IsEligibleNominee)
                {
                    result.Message = "Nominee is not eligible for spot award or not at appropriate level.";
                    return result;
                }

                result.Message = "All validations passed successfully.";
                result.IsQuotaAvailable = true; // Assuming this is part of quota check
                
            }
            catch (Exception ex)
            {
                result.Message = $"Validation error: {ex.Message}";
                result.IsAuthorized = false;
                result.IsNewRequestAllowed = false;
                result.IsEligibleNominee = false;
            }

            return result;
        }

        public int SubmitSpotAwardRequest(SpotAwardRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "Spot award request cannot be null");
            }

            try
            {
                // Validate the request first
                var validation = ValidateSpotAwardRequest(request.InitiatorMEmpID, request.NomineeMEmpID, request.InitiatorMGID);
                
                if (!validation.IsAuthorized || !validation.IsNewRequestAllowed || !validation.IsEligibleNominee)
                {
                    throw new InvalidOperationException($"Request validation failed: {validation.Message}");
                }

                // If validation passes, insert the record
                return _dataAccess.InsertUpdateSpotAward(request);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error submitting spot award request: {ex.Message}");
            }
        }

        public QuotaDetails GetQuotaDetails(int depMGID)
        {
            try
            {
                return _dataAccess.GetDepartmentWiseQuotaDetails(depMGID);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving quota details: {ex.Message}");
            }
        }

        public List<EmployeeDetails> GetEmployeesForNomination(string? employeeName = null)
        {
            try
            {
                return _dataAccess.GetLiveEmployeeDetails(employeeName);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving employee details: {ex.Message}");
            }
        }

        public bool CheckNomineeEligibility(int nomineeMEmpID, int initiatorMEmpID)
        {
            try
            {
                // This method checks if a nominee is eligible based on level criteria
                // It uses the IsNomineeEmpRegularAndUnderEqualLevel method from the data access layer
                // which verifies if the nominee is a regular employee and at or below the initiator's level
                return _dataAccess.IsNomineeEmpRegularAndUnderEqualLevel(nomineeMEmpID, initiatorMEmpID);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking nominee eligibility: {ex.Message}");
            }
        }

        public SpotAwardRequest? GetSpotAwardDetails(int sAID)
        {
            try
            {
                return _dataAccess.GetDataByMasterID(sAID);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving spot award details: {ex.Message}");
            }
        }

        //public List<SpotAwardRequest> GetNominationHistory(int depMGID)
        //{
        //    try
        //    {
        //        return _dataAccess.GetQuotaAndWFDetailsByMGID(depMGID);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"Error retrieving nomination history: {ex.Message}");
        //    }
        //}

        public bool IsUserAuthorized(int initiatorMEmpID)
        {
            try
            {
                if (initiatorMEmpID <= 0)
                {
                    throw new ArgumentException("Employee ID must be greater than zero", nameof(initiatorMEmpID));
                }
                
                return _dataAccess.IsRMPHGHTH(initiatorMEmpID);
            }
            catch (Exception ex)
            {
                // Log the exception details here if you have a logging mechanism
                throw new Exception($"Error checking user authorization: {ex.Message}", ex);
            }
        }
    }
}