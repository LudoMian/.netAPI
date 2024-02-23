using System.Data;
using Dapper;
using DotnetAPI.Data;
using DotnetAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[Controller]")]
    public class PostController : ControllerBase
    {
        private readonly DataContextDapper _dapper;
        public PostController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
        }

        [HttpGet("Posts/{userId}/{postId}/{searchValue}")]
        public IEnumerable<Post> GetPosts(int postId = 0, int userId = 0, string searchValue = "None")
        {
            string sql = "EXEC TutorialAppSchema.spPost_Get";
            string stringParameter = "";

            DynamicParameters sqlParameters = new DynamicParameters();

            if(postId != 0)
            {
                stringParameter += ", @PostId = @PostIdParameter";
                sqlParameters.Add("@PostIdParameter",postId,DbType.Int32);
            }
            if(userId != 0)
            {
                stringParameter += ", @UserId = @UserIdParameter";
                sqlParameters.Add("@UserIdParameter",userId,DbType.Int32);
            }
            // .ToLower() to handle odd casing letters, luckily MSSQL Server is not case sensitive
            if(searchValue.ToLower() != "none")
            {
                stringParameter += ", @SearchValue = SearchValueParameter";
                sqlParameters.Add("@SearchValueParameter",searchValue,DbType.String);
            }
            
            // to handle Substring reduction of empty string
            if(stringParameter.Length > 0)
            {
                sql += stringParameter.Substring(1);
            }

            return _dapper.LoadDataWithParameters<Post>(sql, sqlParameters);
        }

        
        [HttpGet("MyPost")]
        public IEnumerable<Post> GetMyPosts()
        {
            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@UserIdParameter",this.User.FindFirst("UserId")?.Value,DbType.Int32);

            string sql = "EXEC TutorialAppSchema.spPost_Get @UserId =@UserIdParameter";

            return _dapper.LoadDataWithParameters<Post>(sql, sqlParameters);
        }

        [HttpPut("UpsertPost")]
        public IActionResult UpsertPost(Post postToUpsert)
        {

            string sql = @"EXEC TutorialAppSchema.spPosts_Upsert
                @UserId = @UserIdParameter, 
                @PostTitle = @PostTitleParameter, 
                @PostContent = @PostContentParameter"; 

            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@UserIdParameter",this.User.FindFirst("UserId")?.Value,DbType.Int32);
            sqlParameters.Add("@PostTitleParameter",postToUpsert.PostTitle?.Replace("'","''"),DbType.String);
            sqlParameters.Add("@PostContentParameter",postToUpsert.PostContent?.Replace("'","''"),DbType.String);
                // .Replace("'","''") is to handle apostrophe in the content of PostTitle and PostContent 
                
            // PostId in PostTABLE starts with 1
            if(postToUpsert.PostId > 0)
            {
                sql += ", @PostId= @PostIdParameter";
                sqlParameters.Add("@PostIdParameter",postToUpsert.PostId,DbType.Int32);
            }

             // it's gonna check if it was effected atleast one row
            if (_dapper.ExecuteSqlWithParameters(sql, sqlParameters))
            {
                return Ok();
            }
            throw new Exception ("Failed to upsert post!");
        }

        [HttpDelete("Post/{postId}")]
        public IActionResult DeletePost(int postId)
        {
            string sql = @"EXEC TutorialAppSchema.spPost_Delete
                @PostId= @PostIdParameter, 
                @UserId= @UserIdParameter";

            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@PostIdParameter",postId,DbType.Int32);
            sqlParameters.Add("@UserIdParameter",this.User.FindFirst("UserId")?.Value,DbType.Int32);

            if (_dapper.ExecuteSqlWithParameters(sql, sqlParameters))    // it's gonna check if it was effected atleast one row
            {
                return Ok();
            }
            throw new Exception ("Failed to delete post!");

        }

    }
}