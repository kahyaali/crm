using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.DTOs.Auth
{
    public class ChangePasswordRequest
    {
        public int? UserId { get; set; }
        public string? CurrentPassword { get; set; }  // Kendi şifresini değiştirirken gerekli
        public string NewPassword { get; set; } = string.Empty;
    }
}
