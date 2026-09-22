param(
  [Parameter(Mandatory=$true)][string]$Password,
  [string]$ServerInstance = ".\SQLEXPRESS",
  [string]$Database = "BCFixedAsset",
  [string]$UserName = "admin"
)
$ErrorActionPreference = "Stop"
if ($Password.Length -lt 12 -or $Password -notmatch '[A-Z]' -or $Password -notmatch '[a-z]' -or $Password -notmatch '[0-9]' -or $Password -notmatch '[^a-zA-Z0-9]') { throw "Password must be at least 12 characters and include uppercase, lowercase, number and special character." }
$salt = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($salt)
$iterations = 120000
$derive = New-Object Security.Cryptography.Rfc2898DeriveBytes($Password,$salt,$iterations,[Security.Cryptography.HashAlgorithmName]::SHA256)
$hash = $derive.GetBytes(32)
$connection = New-Object Data.SqlClient.SqlConnection("Data Source=$ServerInstance;Initial Catalog=$Database;Integrated Security=True")
$connection.Open()
$command = $connection.CreateCommand()
$command.CommandText = @"
DECLARE @UserId int=(SELECT UserId FROM sec.Users WHERE UserName=@UserName);
IF @UserId IS NULL THROW 50001,'Admin user not found. Run seed script first.',1;
MERGE sec.UserCredentials AS t USING(SELECT @UserId UserId) s ON t.UserId=s.UserId
WHEN MATCHED THEN UPDATE SET PasswordHash=@Hash,PasswordSalt=@Salt,PasswordIterations=@Iterations,PasswordAlgorithm='PBKDF2-HMAC-SHA256',PasswordChangedUtc=SYSUTCDATETIME(),PasswordExpiresUtc=DATEADD(DAY,90,SYSUTCDATETIME()),FailedLoginCount=0,LockedUntilUtc=NULL
WHEN NOT MATCHED THEN INSERT(UserId,PasswordHash,PasswordSalt,PasswordIterations,PasswordAlgorithm,PasswordChangedUtc,PasswordExpiresUtc,FailedLoginCount) VALUES(@UserId,@Hash,@Salt,@Iterations,'PBKDF2-HMAC-SHA256',SYSUTCDATETIME(),DATEADD(DAY,90,SYSUTCDATETIME()),0);
"@
$command.Parameters.Add("@UserName",[Data.SqlDbType]::NVarChar,100).Value=$UserName
$command.Parameters.Add("@Hash",[Data.SqlDbType]::VarBinary,64).Value=$hash
$command.Parameters.Add("@Salt",[Data.SqlDbType]::VarBinary,64).Value=$salt
$command.Parameters.Add("@Iterations",[Data.SqlDbType]::Int).Value=$iterations
$command.ExecuteNonQuery() | Out-Null
$connection.Close()
Write-Host "Initial password configured for $UserName. The user must change it at first login."
