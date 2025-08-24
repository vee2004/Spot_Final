using Microsoft.AspNetCore.Mvc;
using SpotAward.SERVICES;
using SpotAward.Models;
using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.ComponentModel.DataAnnotations;

#nullable enable

namespace SpotAward.CONTROLLERS
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpotAwardController : ControllerBase
    {
        private readonly SpotAwardService _spotAwardService;

        public SpotAwardController(SpotAwardService spotAwardService)
        {
            _spotAwardService = spotAwardService ?? throw new ArgumentNullException(nameof(spotAwardService));
        }
        
        // Method to get the current user's employee ID from the session/authentication context
        // In a real application, this would be implemented to retrieve the user ID from claims, session, etc.
        private int GetCurrentUserMEmpID()
        {
            // This is a placeholder implementation
            // In a real application, you would retrieve this from the authentication context
            // For example: return int.Parse(User.FindFirst("MEmpID")?.Value ?? "0");
            
            // For now, we'll return a default value or extract it from the HttpContext if available
            // You should replace this with actual implementation based on your authentication system
            if (HttpContext.Items.ContainsKey("CurrentUserMEmpID"))
            {
                return (int)HttpContext.Items["CurrentUserMEmpID"];
            }
            
            // Default fallback - in production, you might want to throw an exception instead
            return 0; // Or another appropriate default value
        }

        /// <summary>
        /// Validate if a spot award request is allowed for the initiator
        /// </summary>
        /// <param name="initiatorMEmpID">The employee ID of the initiator</param>
        /// <param name="nomineeMEmpID">The employee ID of the nominee</param>
        /// <param name="initiatorMGID">The management group ID of the initiator</param>
        /// <returns>Validation result with authorization status</returns>
        /// <response code="200">Returns the validation result</response>
        //[HttpGet("validate")]
        //public IActionResult ValidateRequest(
        //    [FromQuery] int initiatorMEmpID,
        //    [FromQuery] int nomineeMEmpID,
        //    [FromQuery] int initiatorMGID)
        //{
        //    var result = _spotAwardService.ValidateRequest(
        //        initiatorMEmpID,
        //        nomineeMEmpID,
        //        initiatorMGID
        //    );

        //    return Ok(new
        //    {
        //        success = result.IsAuthorized && result.IsNewRequestAllowed && result.IsEligibleNominee,
        //        data = result,
        //        message = result.Message
        //    });
        //}

        [HttpPost("submit")]
        public IActionResult SubmitRequest([FromBody] SubmitSpotAwardRequestDTO requestData)
        {
            int newSAID = _spotAwardService.CreateSpotAwardRequest(
                requestData.InitiatorMEmpID,
                requestData.NomineeMEmpID,
                requestData.Justification,
                requestData.InitiatorMGID,
                requestData.IsGHTHInitiated
            );

            return Ok(new
            {
                success = true,
                data = new { SAID = newSAID },
                message = "Spot award request submitted successfully."
            });
        }


        [HttpGet("quota/{depMGID}")]
        public IActionResult GetQuota([FromRoute][System.ComponentModel.DataAnnotations.Required] int depMGID)
        {
            if (depMGID <= 0)
            {
                return BadRequest(new { success = false, message = "Department MGID must be a positive number" });
            }
            
            var quota = _spotAwardService.GetQuotaInformation(depMGID);
            return Ok(new { success = true, data = quota, message = "Quota details retrieved successfully." });
        }

        /// <summary>
        /// Get a list of eligible employee names based on search criteria
        /// </summary>
        /// <param name="name">Search by employee name</param>
        /// <returns>A list of eligible employee names</returns>
        /// <response code="200">Returns the list of eligible employee names</response>
        [HttpGet("employees")]
        [ProducesResponseType(typeof(EmployeeNameListResponse), 200)]
        public IActionResult GetEmployees([FromQuery] string name = null)
        {
            // Get the current user's employee ID from the session/authentication context
            int currentUserMEmpID = GetCurrentUserMEmpID();
            
            // Pass the name parameter directly to the service layer to use in the stored procedure
            var employees = _spotAwardService.GetEmployeeList(name);
            
            // Check eligibility for all employees
            var eligibleEmployees = new List<EmployeeDetails>();
            foreach (var employee in employees)
            {
                // Check if nominee is eligible based on level criteria
                bool isEligible = _spotAwardService.CheckNomineeEligibility(employee.MEmpID, currentUserMEmpID);
                if (isEligible)
                {
                    eligibleEmployees.Add(employee);
                }
            }
            
            // Extract only employee names from eligible employees
            var employeeNames = eligibleEmployees.Select(e => e.EmployeeName).ToList();
            
            // Return using the specific response type for Swagger documentation
            return Ok(new EmployeeNameListResponse { 
                Success = true, 
                Data = employeeNames, 
                Message = "Eligible employee names retrieved successfully." 
            });
        }

        [HttpGet("details/{sAID}")]
        public IActionResult GetRequestDetails(int sAID)
        {
            var details = _spotAwardService.GetRequestDetails(sAID);
            if (details == null) return NotFound();
            return Ok(new { success = true, data = details, message = "Request details retrieved successfully." });
        }

        //[HttpGet("history/{depMGID}")]
        //public IActionResult GetHistory(int depMGID)
        //{
        //    var history = _spotAwardService.GetDepartmentHistory(depMGID);
        //    return Ok(new { success = true, data = history, message = "Department history retrieved successfully." });
        //}

        /// <summary>
        /// Check if the user is authorized to initiate the request (RM/PH/GH/TH)
        /// </summary>
        /// <param name="initiatorMEmpID">The employee ID of the initiator</param>
        /// <returns>True if user is authorized, false otherwise</returns>
        /// <response code="200">Returns authorization status</response>
        [HttpGet("isAuthorized")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public IActionResult IsRMPHGHTH([FromQuery] int initiatorMEmpID)
        {
            try
            {
                bool isAuthorized = _spotAwardService.IsUserAuthorized(initiatorMEmpID);
                
                return Ok(new
                {
                    success = true,
                    data = isAuthorized,
                    message = isAuthorized ? "User is authorized." : "User is not authorized."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    data = false,
                    message = $"Error checking authorization: {ex.Message}"
                });
            }
        }
    }
}
