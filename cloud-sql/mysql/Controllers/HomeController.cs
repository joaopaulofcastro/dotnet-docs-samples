/*
 * Copyright (c) 2019 Google LLC.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 */

using CloudSql.ViewModels;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Threading.Tasks;

namespace CloudSql.Controllers
{
    public class HomeController : Controller
    {
       private readonly MySqlConnectionStringBuilder _connectionString;

        public HomeController(
            MySqlConnectionStringBuilder connectionString)
        {
            this._connectionString = connectionString;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Initialize the Model that will be passed to the View.
            HomeModel model = new HomeModel()
            {
                VoteEntry = new List<VoteEntry>()
            };
            using(var connection = new MySqlConnection(_connectionString.ConnectionString))
            { 
                connection.OpenWithRetry();
                // Look up the last 5 votes.
                using (var lookupCommand = connection.CreateCommand())
                {
                    lookupCommand.CommandText = @"
                        SELECT candidate, time_cast FROM votes ORDER BY time_cast DESC LIMIT 5";
                    using (var reader = await lookupCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            model.VoteEntry.Add(new VoteEntry()
                            {
                                Candidate = reader.GetString(0),
                                TimeCast = reader.GetDateTime(1)
                            });
                        }
                    }
                }
                using (var countsCommand = connection.CreateCommand())
                {
                    countsCommand.CommandText = @"
                        SELECT COUNT(vote_id) FROM votes WHERE candidate= @candidate";
                    var candidate = countsCommand.CreateParameter();
                    candidate.ParameterName = "@candidate";
                    candidate.DbType = DbType.String;

                    candidate.Value = "SPACES";
                    countsCommand.Parameters.Add(candidate);
                    using (var reader = await countsCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            model.SpaceCount = reader.GetInt32(0);
                        }
                    }
                    countsCommand.Parameters.Clear();
                    candidate.Value = "TABS";
                    countsCommand.Parameters.Add(candidate);
                    using (var reader = await countsCommand.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            model.TabCount = reader.GetInt32(0);
                        }
                    }
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index([FromForm] string team)
        {
            bool validInput = true;
            DateTime insertTimestamp;
            if (!String.IsNullOrEmpty(team))
            {
                team = team.ToUpper();
                if (!team.Equals("SPACES") && !team.Equals("TABS"))
                {
                    validInput = false;
                }
            }
            else
            {
                validInput = false;
            }

            if (validInput)
            {
                // [START cloud_sql_mysql_dotnet_ado_create]
                insertTimestamp = DateTime.UtcNow;
                try
                {
                    using(var connection = new MySqlConnection(_connectionString.ConnectionString))
                    { 
                        connection.OpenWithRetry();
                        using (var insertVoteCommand = connection.CreateCommand())
                        {
                            insertVoteCommand.CommandText =
                                @"INSERT INTO votes (candidate, time_cast) VALUES (@candidate, @time_cast)";
                            var candidate = insertVoteCommand.CreateParameter();
                            candidate.ParameterName = "@candidate";
                            candidate.DbType = DbType.String;
                            candidate.Value = team;
                            insertVoteCommand.Parameters.Add(candidate);
                            var timeCast = insertVoteCommand.CreateParameter();
                            timeCast.ParameterName = "@time_cast";
                            timeCast.DbType = DbType.DateTime;
                            timeCast.Value = insertTimestamp;
                            insertVoteCommand.Parameters.Add(timeCast);
                            await insertVoteCommand.ExecuteNonQueryAsync();
                        }
                    }
                    return Content($"Vote successfully cast for '{team}' at time {insertTimestamp}!");
                }
                catch (Exception ex)
                {
                    // If something goes wrong, handle the error in this
                    // section. This might involve retrying or adjusting
                    // parameters depending on the situation.
                    return StatusCode((int)HttpStatusCode.InternalServerError, ex);
                }
                // [END cloud_sql_mysql_dotnet_ado_create]
            }
            else
            {
                return Content("Invalid team specified.");
            }
        }
    }
}
