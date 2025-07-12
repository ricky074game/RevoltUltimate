using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevoltUltimate.API.Accounts
{
    public class StoredAccount
    {
        public string Username { get; set; }
        public byte[] EncryptedGuardData { get; set; }
        public byte[] EncryptedPassword { get; set; }
    }
}
