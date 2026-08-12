using System;
using System.Collections.Generic;
using System.Text;


namespace SEAL_Application.Features.Teams.Commands.CreateTeam.Models
{
    public class CreateTeamRequestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;  
        public string LeaderId { get; set; } = string.Empty; 
    }
}