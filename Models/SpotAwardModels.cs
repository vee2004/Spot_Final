using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

#nullable enable

namespace SpotAward.Models
{
    public class SpotAwardRequest
    {
        public int SAID { get; set; }
        public int InitiatorMEmpID { get; set; }
        public int NomineeMEmpID { get; set; }
        public string Justification { get; set; } = string.Empty;
        public bool IsGHTHInitiated { get; set; }
        public bool IsDelivered { get; set; }
        public int InitiatorMGID { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    public class QuotaDetails
    {
        public int HeadCount { get; set; }
        public int IssuedCount { get; set; }
        public int AwardInPipeline { get; set; }
        public int BalanceCount { get; set; }
        public int AllottedQuota { get; set; }
    }

    public class ValidationResult
    {
        public bool IsAuthorized { get; set; }
        public bool IsQuotaAvailable { get; set; }
        public bool IsEligibleNominee { get; set; }
        public bool IsNewRequestAllowed { get; set; }
        public string Message { get; set; } = string.Empty;
        public int DepMGID { get; set; }
    }

    public class EmployeeDetails
    {
        public int MEmpID { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int MGID { get; set; }
        public string Department { get; set; } = string.Empty;
        public int Level { get; set; }
    }

    // ✅ DTOs for POST requests
    public class SubmitSpotAwardRequestDTO
    {
        [Required]
        public int InitiatorMEmpID { get; set; }

        [Required]
        public int NomineeMEmpID { get; set; }

        [Required]
        public int InitiatorMGID { get; set; }

        [Required]
        [StringLength(500)]
        public string Justification { get; set; }

        public bool IsGHTHInitiated { get; set; } = false;
    }

    public class ValidateSpotAwardRequestDTO
    {
        [Required]
        public int InitiatorMEmpID { get; set; }

        [Required]
        public int NomineeMEmpID { get; set; }

        [Required]
        public int InitiatorMGID { get; set; }
    }

    public class EmployeeNameDTO
    {
        public string? EmployeeName { get; set; }
    }

    public class EmployeeNameListResponse
    {
        public bool Success { get; set; }
        public List<string> Data { get; set; } = new List<string>();
        public string Message { get; set; } = string.Empty;
    }
}
