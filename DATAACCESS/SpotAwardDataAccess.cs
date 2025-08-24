using System;
using System.Data;
using Microsoft.Data.SqlClient; // instead of System.Data.SqlClient
using SpotAward.Models;
using System.Collections.Generic;

#nullable enable

namespace SpotAward.DATAACCESS
{
    public class SpotAwardDataAccess
    {
        private readonly string _connectionString;

        public SpotAwardDataAccess(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty");
            }
            _connectionString = connectionString;
        }

        // Function 1: Check if user is authorized to initiate request
        public bool IsRMPHGHTH(int initiatorMEmpID)
        {
            bool isRM = false;
            bool isGHTH = false;
            
            try
            {
                if (initiatorMEmpID <= 0)
                {
                    throw new ArgumentException("Employee ID must be greater than zero", nameof(initiatorMEmpID));
                }
                
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SpotAwardWF_IsRMPHGHTH", connection);
                    cmd.CommandType = CommandType.StoredProcedure;
                    
                    cmd.Parameters.AddWithValue("@InitiatorMEmpID", initiatorMEmpID);
                    
                    try
                    {
                        connection.Open();
                    }
                    catch (SqlException sqlEx)
                    {
                        throw new Exception($"Database connection error: {sqlEx.Message}", sqlEx);
                    }
                    
                    try
                    {
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        
                        if (dt.Rows.Count > 0)
                        {
                            foreach (DataRow dr in dt.Rows)
                            {
                                // Check if columns exist before accessing them
                                if (dt.Columns.Contains("ISRM") && dt.Columns.Contains("ISGHTH"))
                                {
                                    isRM = dr["ISRM"] != DBNull.Value ? Convert.ToBoolean(dr["ISRM"]) : false;
                                    isGHTH = dr["ISGHTH"] != DBNull.Value ? Convert.ToBoolean(dr["ISGHTH"]) : false;
                                }
                                else
                                {
                                    throw new Exception("Required columns (ISRM, ISGHTH) not found in the result set");
                                }
                            }
                        }
                        
                        dt.Dispose();
                    }
                    finally
                    {
                        cmd.Dispose();
                        if (connection.State == ConnectionState.Open)
                        {
                            connection.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception details here if you have a logging mechanism
                throw new Exception($"Error checking authorization: {ex.Message}", ex);
            }
            
            return isRM || isGHTH;
        }

        // Function 2: Load quota details for department
        public QuotaDetails GetDepartmentWiseQuotaDetails(int depMGID)
{
    QuotaDetails quotaDetails = new QuotaDetails();

    // ✅ Always use current year
    int year = DateTime.Now.Year;

    try
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            SqlCommand cmd = new SqlCommand("SpotAwardWF_GetAllQuotaDetails", connection);
            cmd.CommandType = CommandType.StoredProcedure;

            // ✅ Parameters
            cmd.Parameters.AddWithValue("@DepMGID", depMGID);
            cmd.Parameters.AddWithValue("@Year", year);

            connection.Open();

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    quotaDetails.DeptName        = reader["DeptName"]        != DBNull.Value ? reader["DeptName"].ToString() : string.Empty;
                    quotaDetails.HeadCount       = reader["HeadCount"]       != DBNull.Value ? Convert.ToInt32(reader["HeadCount"]) : 0;
                    quotaDetails.IssuedCount     = reader["IssuedCount"]     != DBNull.Value ? Convert.ToInt32(reader["IssuedCount"]) : 0;
                    quotaDetails.AwardInPipeline = reader["AwardInPipeline"] != DBNull.Value ? Convert.ToInt32(reader["AwardInPipeline"]) : 0;
                    quotaDetails.BalanceCount    = reader["BalanceCount"]    != DBNull.Value ? Convert.ToInt32(reader["BalanceCount"]) : 0;
                    quotaDetails.AllottedQuota   = reader["AllottedQuota"]   != DBNull.Value ? Convert.ToInt32(reader["AllottedQuota"]) : 0;
                }
            }
        }
    }
    catch (Exception ex)
    {
        throw new Exception($"Error loading quota details: {ex.Message}");
    }

    return quotaDetails;
}


        // Function 3: Get employee details for nomination (autocomplete)
        public List<EmployeeDetails> GetLiveEmployeeDetails(string? employeeName = null)
        {
            List<EmployeeDetails> employees = new List<EmployeeDetails>();
            
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("WFSRV_LiveEMPDetails", connection);
                    cmd.CommandType = CommandType.StoredProcedure;
                    
                    // Add employee name parameter if provided
                    if (!string.IsNullOrEmpty(employeeName))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeName", employeeName);
                    }
                    
                    connection.Open();
                    
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        employees.Add(new EmployeeDetails
                        {
                            MEmpID = Convert.ToInt32(reader["MEmpID"]),
                            EmployeeName = reader["EmployeeName"]?.ToString() ?? string.Empty,
                            MGID = Convert.ToInt32(reader["MGID"]),
                            Department = reader["Department"]?.ToString() ?? string.Empty,
                            Level = Convert.ToInt32(reader["Level"])
                        });
                    }
                    
                    reader.Close();
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading employee details: {ex.Message}");
            }
            
            return employees;
        }

        // Function 4a: Check if new request is allowed based on quota
        public ValidationResult IsNewRequestAllowed(int initiatorMEmpID, int initiatorMGID)
        {
            ValidationResult result = new ValidationResult();
            
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SpotAwardWF_IsNewRequestAllowed", connection);
                    cmd.CommandType = CommandType.StoredProcedure;
                    
                    cmd.Parameters.AddWithValue("@InitiatorMEmpID", initiatorMEmpID);
                    cmd.Parameters.AddWithValue("@InitiatorMGID", initiatorMGID);
                    
                    // Output parameters
                    SqlParameter statusParam = new SqlParameter("@NewRequestStatus", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(statusParam);
                    
                    SqlParameter msgParam = new SqlParameter("@Msg", SqlDbType.NVarChar, -1)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(msgParam);
                    
                    SqlParameter depMGIDParam = new SqlParameter("@DepMGID", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(depMGIDParam);
                    
                    connection.Open();
                    cmd.ExecuteNonQuery();
                    
                    result.IsNewRequestAllowed = Convert.ToInt32(statusParam.Value) == 1;
                    result.Message = msgParam.Value?.ToString() ?? "";
                    result.DepMGID = Convert.ToInt32(depMGIDParam.Value);
                    
                    connection.Close();
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                result.IsNewRequestAllowed = false;
                result.Message = $"Error checking request allowance: {ex.Message}";
            }
            
            return result;
        }

        // Function 4b: Check if nominee is regular and under/equal level of initiator
        public bool IsNomineeEmpRegularAndUnderEqualLevel(int nomineeMEmpID, int initiatorMEmpID)
        {
            bool isEligible = false;
            
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SpotAwardWF_IsNomineeEmpRegularAndUnderEqualLevel", connection);
                    cmd.CommandType = CommandType.StoredProcedure;
                    
                    cmd.Parameters.AddWithValue("@NomineeMEmpID", nomineeMEmpID);
                    cmd.Parameters.AddWithValue("@InitiatorMEmpID", initiatorMEmpID);
                    
                    connection.Open();
                    
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    
                    foreach (DataRow dr in dt.Rows)
                    {
                        isEligible = Convert.ToBoolean(dr["IsNomineeEmpRegularAndUnderEqualLevel"]);
                    }
                    
                    dt.Dispose();
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking nominee eligibility: {ex.Message}");
            }
            
            return isEligible;
        }

        // Function 4d: Insert/Update spot award nomination
        public int InsertUpdateSpotAward(SpotAwardRequest request)
        {
            int newSAID = 0;
            
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SpotAwardWF_Master_InsertUpdate", connection);
                    cmd.CommandType = CommandType.StoredProcedure;
                    
                    cmd.Parameters.AddWithValue("@SAID", request.SAID);
                    cmd.Parameters.AddWithValue("@InitiatorMEmpID", request.InitiatorMEmpID);
                    cmd.Parameters.AddWithValue("@NomineeMEmpID", request.NomineeMEmpID);
                    cmd.Parameters.AddWithValue("@Justification", request.Justification);
                    cmd.Parameters.AddWithValue("@ISGHTHInitiated", request.IsGHTHInitiated);
                    cmd.Parameters.AddWithValue("@IsDelivered", request.IsDelivered);
                    cmd.Parameters.AddWithValue("@InitiatorMGID", request.InitiatorMGID);
                    
                    connection.Open();
                    
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        newSAID = Convert.ToInt32(result);
                    }
                    
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inserting/updating spot award: {ex.Message}");
            }
            
            return newSAID;
        }

        // Function 5: Load data from local DB based on master ID
        public SpotAwardRequest? GetDataByMasterID(int sAID)
        {
            SpotAwardRequest? request = null;
            
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    SqlCommand cmd = new SqlCommand("SpotAwardWF_GetDataByMasterID", connection);
                    cmd.CommandType = CommandType.StoredProcedure;
                    
                    cmd.Parameters.AddWithValue("@SAID", sAID);
                    
                    connection.Open();
                    
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        request = new SpotAwardRequest
                        {
                            SAID = Convert.ToInt32(reader["SAID"]),
                            InitiatorMEmpID = Convert.ToInt32(reader["InitiatorMEmpID"]),
                            NomineeMEmpID = Convert.ToInt32(reader["NomineeMEmpID"]),
                            Justification = reader["Justification"]?.ToString() ?? string.Empty,
                            IsGHTHInitiated = Convert.ToBoolean(reader["IsGHTHInitiated"]),
                            IsDelivered = Convert.ToBoolean(reader["IsDelivered"]),
                            InitiatorMGID = Convert.ToInt32(reader["InitiatorMGID"]),
                            CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                            ModifiedDate = reader["ModifiedDate"] != DBNull.Value ? 
                                          Convert.ToDateTime(reader["ModifiedDate"]) : null
                        };
                    }
                    
                    reader.Close();
                    cmd.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading spot award data: {ex.Message}");
            }
            
            return request;
        }

        // Get nomination history for department
        //public List<SpotAwardRequest> GetQuotaAndWFDetailsByMGID(int depMGID)
        //{
        //    List<SpotAwardRequest> nominations = new List<SpotAwardRequest>();
            
        //    try
        //    {
        //        using (SqlConnection connection = new SqlConnection(_connectionString))
        //        {
        //            SqlCommand cmd = new SqlCommand("SpotAwardWF_GetQuotaANDWFDetailsByMGID", connection);
        //            cmd.CommandType = CommandType.StoredProcedure;
                    
        //            cmd.Parameters.AddWithValue("@DepMGID", depMGID);
                    
        //            connection.Open();
                    
        //            SqlDataReader reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                nominations.Add(new SpotAwardRequest
        //                {
        //                    SAID = Convert.ToInt32(reader["SAID"]),
        //                    InitiatorMEmpID = Convert.ToInt32(reader["InitiatorMEmpID"]),
        //                    NomineeMEmpID = Convert.ToInt32(reader["NomineeMEmpID"]),
        //                    Justification = reader["Justification"]?.ToString() ?? string.Empty,
        //                    IsGHTHInitiated = Convert.ToBoolean(reader["IsGHTHInitiated"]),
        //                    IsDelivered = Convert.ToBoolean(reader["IsDelivered"]),
        //                    InitiatorMGID = Convert.ToInt32(reader["InitiatorMGID"]),
        //                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
        //                });
        //            }
                    
        //            reader.Close();
        //            cmd.Dispose();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"Error loading nomination history: {ex.Message}");
        //    }
            
        //    return nominations;
        //}
    }
}
