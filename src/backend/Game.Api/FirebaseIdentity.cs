using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Game.Application;
using Newtonsoft.Json.Linq;
namespace Game.Api;
public sealed class FirebaseIdentity(IConfiguration configuration,IHostEnvironment environment) : IDisposable
{
 private FirebaseApp? app;private readonly object gate=new();
 public async Task<AuthIdentity> Verify(string? token)
 {
  if(string.IsNullOrWhiteSpace(token))throw new SessionFault(SessionFailure.Forbidden,"Sign in before joining or changing this household.");
  var project=configuration["Firebase:ProjectId"];
  if(string.IsNullOrWhiteSpace(project))throw new SessionFault(SessionFailure.Forbidden,"Household authentication is not configured.");
  var emulator=Environment.GetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST");
  if(emulator is not null&&(!environment.IsDevelopment()||!project.StartsWith("demo-",StringComparison.Ordinal)))throw new SessionFault(SessionFailure.Forbidden,"Emulator authentication requires Development and a demo project.");
  lock(gate)app??=FirebaseApp.Create(new AppOptions{ProjectId=project,Credential=emulator is not null?GoogleCredential.FromAccessToken("local-emulator-placeholder"):GoogleCredential.GetApplicationDefault()},Guid.NewGuid().ToString());
  try{var decoded=await FirebaseAuth.GetAuth(app).VerifyIdTokenAsync(token);var provider=decoded.Claims.TryGetValue("firebase",out var claim)?(claim as JObject)?["sign_in_provider"]?.ToString():null;
   if(provider is not ("anonymous" or "password"))throw new SessionFault(SessionFailure.Forbidden,"Use an email/password account or anonymous guest identity.");
   return new(decoded.Uid,provider=="anonymous");}
  catch(Exception error) when(error is FirebaseAuthException or ArgumentException){throw new SessionFault(SessionFailure.Forbidden,"Sign-in token is invalid or expired. Sign in again.");}
 }
 public void Dispose(){app?.Delete();}
}
