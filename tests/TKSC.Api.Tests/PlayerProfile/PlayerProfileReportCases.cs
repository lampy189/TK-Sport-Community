using be_da.Application.DTOs;
using be_da.Application.Interfaces.External;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Services;
using be_da.Infrastructure.Persistence.Repositories;
using Xunit;
using Xunit.Abstractions;

namespace TKSC.Api.Tests.UnitTests.PlayerProfile;

// Report 5.1 – PlayerProfile (Lê Tuấn Dũng): edit personal profile and map the initial skill survey score to a band
// (RP3 UC-03, UC-04 / UC-06; sequence "Player Profile and Initial Skill Survey").
public sealed class PlayerProfileReportCases(ITestOutputHelper output) : ReportCaseOutput(output)
{
    private const string NameLength = "Invalid: Họ và tên phải có độ dài từ 2 đến 100 ký tự.";
    private const string BirthDate = "Invalid: Ngày sinh không hợp lệ (không được ở tương lai hoặc trước năm 1920).";

    private static UpdateProfileRequest Profile(string? name = "Lê Tuấn Dũng", DateOnly? dob = null, string? gender = "Male", string? avatar = null, string? bio = "Thích cầu lông") =>
        new(name, dob ?? new DateOnly(2003, 5, 12), gender, avatar, bio);

    // ---------------- ProfileService.UpdateProfileAsync
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Player is signed in and has a profile")]
    public Task UTC001_ValidProfile() =>
        CheckAsync("ProfileService.UpdateProfileAsync", "request=UpdateProfileRequest(FullName=\"Lê Tuấn Dũng\", DateOfBirth=2003-05-12, Gender=\"Male\", Bio=\"Thích cầu lông\")",
            "Success", () => Update(Profile()));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Full name left empty")]
    public Task UTC002_EmptyName() =>
        CheckAsync("ProfileService.UpdateProfileAsync", "request=UpdateProfileRequest(FullName=\"\")",
            "Invalid: Họ và tên không được để trống.", () => Update(Profile(name: "")));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Full name of 1 character")]
    public Task UTC003_NameOneCharacter() =>
        CheckAsync("ProfileService.UpdateProfileAsync", "request=UpdateProfileRequest(FullName=\"A\")", NameLength, () => Update(Profile(name: "A")));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Full name of 2 characters")]
    public Task UTC004_NameTwoCharacters() =>
        CheckAsync("ProfileService.UpdateProfileAsync", "request=UpdateProfileRequest(FullName=\"An\")", "Success", () => Update(Profile(name: "An")));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Full name of 100 characters")]
    public Task UTC005_NameHundredCharacters() =>
        CheckAsync("ProfileService.UpdateProfileAsync", "request=UpdateProfileRequest(FullName=string('a', 100))", "Success", () => Update(Profile(name: new string('a', 100))));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Full name of 101 characters")]
    public Task UTC006_NameTooLong() =>
        CheckAsync("ProfileService.UpdateProfileAsync", "request=UpdateProfileRequest(FullName=string('a', 101))", NameLength, () => Update(Profile(name: new string('a', 101))));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Date of birth in the future")]
    public Task UTC007_BirthDateInFuture() =>
        CheckAsync("ProfileService.UpdateProfileAsync", "request=UpdateProfileRequest(DateOfBirth=2099-01-01)", BirthDate, () => Update(Profile(dob: new DateOnly(2099, 1, 1))));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Date of birth one day before 1920")]
    public Task UTC008_BirthDateBefore1920() =>
        CheckAsync("ProfileService.UpdateProfileAsync", "request=UpdateProfileRequest(DateOfBirth=1919-12-31)", BirthDate, () => Update(Profile(dob: new DateOnly(1919, 12, 31))));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Date of birth on 01/01/1920")]
    public Task UTC009_BirthDateFirstDay1920() =>
        CheckAsync("ProfileService.UpdateProfileAsync", "request=UpdateProfileRequest(DateOfBirth=1920-01-01)", "Success", () => Update(Profile(dob: new DateOnly(1920, 1, 1))));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Gender entered in Vietnamese")]
    public Task UTC010_GenderVietnamese() =>
        CheckAsync("ProfileService.UpdateProfileAsync", "request=UpdateProfileRequest(Gender=\"Nữ\")", "Success", () => Update(Profile(gender: "Nữ")));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Gender value not in the allowed list")]
    public Task UTC011_GenderUnknown() =>
        CheckAsync("ProfileService.UpdateProfileAsync", "request=UpdateProfileRequest(Gender=\"Unknown\")",
            "Invalid: Giới tính không hợp lệ (Male, Female, Other).", () => Update(Profile(gender: "Unknown")));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Bio of 500 characters")]
    public Task UTC012_BioMaximum() =>
        CheckAsync("ProfileService.UpdateProfileAsync", "request=UpdateProfileRequest(Bio=string('a', 500))", "Success", () => Update(Profile(bio: new string('a', 500))));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Bio of 501 characters")]
    public Task UTC013_BioTooLong() =>
        CheckAsync("ProfileService.UpdateProfileAsync", "request=UpdateProfileRequest(Bio=string('a', 501))",
            "Invalid: Giới thiệu bản thân không được vượt quá 500 ký tự.", () => Update(Profile(bio: new string('a', 501))));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Avatar given as an HTTPS link")]
    public Task UTC014_AvatarHttpsUrl() =>
        CheckAsync("ProfileService.UpdateProfileAsync", "request=UpdateProfileRequest(AvatarUrl=\"https://cdn.tksc.vn/avatar/dung.png\")",
            "Success", () => Update(Profile(avatar: "https://cdn.tksc.vn/avatar/dung.png")));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Avatar link that is not HTTP or HTTPS")]
    public Task UTC015_AvatarFtpUrl() =>
        CheckAsync("ProfileService.UpdateProfileAsync", "request=UpdateProfileRequest(AvatarUrl=\"ftp://files.tksc.vn/dung.png\")",
            "Invalid: Avatar URL must be a valid HTTP or HTTPS URL.", () => Update(Profile(avatar: "ftp://files.tksc.vn/dung.png")));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Player uploads an image; storage saves the file")]
    public Task UTC016_AvatarUploadSaved() =>
        CheckAsync("ProfileService.UpdateProfileAsync", "request=UpdateProfileRequest(AvatarUrl=\"data:image/png;base64,iVBORw0KGgo=\")",
            "Success: avatar=/api/profile/avatars/7-upload.png", () => Update(Profile(avatar: "data:image/png;base64,iVBORw0KGgo="), showAvatar: true));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Uploaded image cannot be decoded; storage rejects it")]
    public Task UTC017_AvatarUploadRejected() =>
        CheckAsync("ProfileService.UpdateProfileAsync", "request=UpdateProfileRequest(AvatarUrl=\"data:image/png;base64,not-an-image\")",
            "Invalid: Ảnh đại diện tải lên không hợp lệ.", () => Update(Profile(avatar: "data:image/png;base64,not-an-image")));

    // ---------------- SqlSkillProfileStore.MapScoreToBand (initial survey: score = 5 x sum of 5 answers)
    [Fact, Trait("Type", "Boundary"), Trait("Pre", "All 5 survey answers at level 0")]
    public void UTC018_ScoreZero() => Check("SqlSkillProfileStore.MapScoreToBand", "score=0", "B1", () => SqlSkillProfileStore.MapScoreToBand(0));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Highest score of band B1")]
    public void UTC019_Score19() => Check("SqlSkillProfileStore.MapScoreToBand", "score=19", "B1", () => SqlSkillProfileStore.MapScoreToBand(19));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Lowest score of band B2")]
    public void UTC020_Score20() => Check("SqlSkillProfileStore.MapScoreToBand", "score=20", "B2", () => SqlSkillProfileStore.MapScoreToBand(20));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Answers 2, 2, 2, 2, 1 (score 45)")]
    public void UTC021_Score45() => Check("SqlSkillProfileStore.MapScoreToBand", "score=45", "B3", () => SqlSkillProfileStore.MapScoreToBand(45));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Highest score of band B4")]
    public void UTC022_Score79() => Check("SqlSkillProfileStore.MapScoreToBand", "score=79", "B4", () => SqlSkillProfileStore.MapScoreToBand(79));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Lowest score of band B5")]
    public void UTC023_Score80() => Check("SqlSkillProfileStore.MapScoreToBand", "score=80", "B5", () => SqlSkillProfileStore.MapScoreToBand(80));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "All 5 survey answers at level 4 (score 100)")]
    public void UTC024_Score100() => Check("SqlSkillProfileStore.MapScoreToBand", "score=100", "B5", () => SqlSkillProfileStore.MapScoreToBand(100));

    private static async Task<string> Update(UpdateProfileRequest request, bool showAvatar = false)
    {
        var store = new FakeProfileStore();
        var result = await new ProfileService(store, new FakeAvatarStorage()).UpdateProfileAsync(7, request, default);
        if (result.Outcome == OperationOutcome.Success)
            return showAvatar ? $"Success: avatar={store.Saved!.AvatarUrl}" : "Success";
        return $"{result.Outcome}: {result.Value!.GetType().GetProperty("message")!.GetValue(result.Value)}";
    }

    private sealed class FakeAvatarStorage : IAvatarStorage
    {
        public Task<string?> SaveDataUrlAsync(long userId, string dataUrl, CancellationToken cancellationToken) =>
            Task.FromResult(dataUrl.Contains("not-an-image") ? null : $"/api/profile/avatars/{userId}-upload.png");
        public AvatarFile? Find(string fileName) => null;
    }

    private sealed class FakeProfileStore : IProfileStore
    {
        public UpdateProfileRequest? Saved { get; private set; }

        public Task<UserProfileDetails?> UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken cancellationToken)
        {
            Saved = request;
            return Task.FromResult<UserProfileDetails?>(new(userId, "player.vp02@tksc.local", request.FullName!.Trim(), "Player", "Active",
                request.DateOfBirth, request.Gender, request.AvatarUrl, [], 20075, request.Bio));
        }

        public Task<UserProfileDetails?> GetProfileAsync(long userId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PublicPlayerProfileDetails?> GetPublicPlayerProfileAsync(long playerId, long? viewerUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<SportProfileItem>?> GetSportProfilesAsync(long userId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<SportProfileResult> UpdateSportProfileAsync(long userId, string sportCode, UpdateSportProfileRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
