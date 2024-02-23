using System.Data;
using Dapper;
using DotnetAPI.Data;
using DotnetAPI.Helpers;
using DotnetAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetAPI.Controllers;

[Authorize]
[ApiController]         //atribute for our Class "controller" ,if we look into file this is what we wanna map to our Endpoints
[Route("[controller]")] // it will get rid off controller from UserController name of class and use it in ROUTE

public class UserCompleteController : ControllerBase
{
    private readonly DataContextDapper _dapper;
    private readonly ReusableSql _reusableSql;
    public UserCompleteController(IConfiguration config)
    {
        // Console.WriteLine(config.GetConnectionString("DefaultConnection"));
        _dapper = new DataContextDapper(config);
        _reusableSql = new ReusableSql(config);
    }

    [HttpGet("TestConnection")] // ENDPOIT
    public DateTime TestConnection()
    {
        return _dapper.LoadDataSingle<DateTime>("SELECT GETDATE()");
    }

    [HttpGet("GetUsers/{userId}/{isActive}")] 
    public IEnumerable<UserComplete> GetUsers(int userId, bool? isActive) // public/private <EXPECTED RETURN> ConstructorName ()
    {
        // Execution of procedure User GET to get all information also from Salary an Jobinfo TABLE
        string sql = @"EXEC TutorialAppSchema.spUsers_Get";
        string stringParameters="";

        // to prevent sql injection we setup dynamic parameters
        DynamicParameters sqlParameters =new DynamicParameters();
        
        if(userId!=0){
            stringParameters += ", @UserId=@UserIdParameter"; 
            sqlParameters.Add("@UserIdParameter", userId, DbType.Int32); 
        }

        // I change the if(isActive) to handle False Active value Users 
        if(isActive.HasValue){
            stringParameters += ", @Active=@ActiveParameter";
            sqlParameters.Add("@ActiveParameter", isActive, DbType.Boolean); 
            // parameters += ", @Active=" + (isActive.Value ? "1" : "0");
        }
        // to remove coma from 1st parameter added to SQL string => with .Substring(1) it start on index[1] instead of [0] to the end of string 
        if(stringParameters.Length > 0)
        {
            sql += stringParameters.Substring(1);  // same as (1,parameters.Length);
        }
        
        IEnumerable<UserComplete> users = _dapper.LoadDataWithParameters<UserComplete>(sql, sqlParameters);
        return users;
    }

    [HttpPut("UpsertUser")]
    public IActionResult UpsertUser(UserComplete user) // will be looking for UserComplete Modul
    {
        if (_reusableSql.UpsertUser(user))
        {
            return Ok(); // inhereted method from ControllerBase, it tells user if request was succesfull 
        }
        throw new Exception("Failed to Upsert User");
    }

    [HttpDelete("DeleteUser/{userId}")]
    public IActionResult DeleteUser(int userId)
    {
        string sql = "EXEC TutorialAppSchema.spUser_Delete @UserId=@UserIdParameter";
        
        DynamicParameters sqlParameters =new DynamicParameters();
        sqlParameters.Add("@UserIdParameter", userId, DbType.Int32);
 
        if (_dapper.ExecuteSqlWithParameters(sql, sqlParameters))
        {
            return Ok(); // inhereted method from ControllerBase, it tells user if request was succesfull 
        }
        throw new Exception("Failed to Delete User");
    }
}