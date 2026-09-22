param(
  [Parameter(Mandatory=$true)][string]$Password,
  [string]$ServerInstance = ".\SQLEXPRESS",
  [string]$Database = "BCFixedAsset",
  [string]$UserName = "admin"
)
$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($Password)) { throw "Password is required." }
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
WHEN MATCHED THEN UPDATE SET PasswordHash=@Hash,PasswordSalt=@Salt,PasswordIterations=@Iterations,PasswordAlgorithm='PBKDF2-HMAC-SHA256',PasswordChangedUtc=SYSUTCDATETIME(),PasswordExpiresUtc=NULL,FailedLoginCount=0,LockedUntilUtc=NULL
WHEN NOT MATCHED THEN INSERT(UserId,PasswordHash,PasswordSalt,PasswordIterations,PasswordAlgorithm,PasswordChangedUtc,PasswordExpiresUtc,FailedLoginCount) VALUES(@UserId,@Hash,@Salt,@Iterations,'PBKDF2-HMAC-SHA256',SYSUTCDATETIME(),NULL,0);
UPDATE sec.Users SET MustChangePassword=0 WHERE UserId=@UserId;
"@
$command.Parameters.Add("@UserName",[Data.SqlDbType]::NVarChar,100).Value=$UserName
$command.Parameters.Add("@Hash",[Data.SqlDbType]::VarBinary,64).Value=$hash
$command.Parameters.Add("@Salt",[Data.SqlDbType]::VarBinary,64).Value=$salt
$command.Parameters.Add("@Iterations",[Data.SqlDbType]::Int).Value=$iterations
$command.ExecuteNonQuery() | Out-Null
$connection.Close()
Write-Host "Password configured for $UserName."
