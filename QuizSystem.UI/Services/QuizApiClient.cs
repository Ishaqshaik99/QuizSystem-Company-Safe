using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using QuizSystem.Core.Common;
using QuizSystem.Core.DTOs;
using QuizSystem.UI.Auth;

namespace QuizSystem.UI.Services;

public class QuizApiClient
{
    private readonly HttpClient _httpClient;
    private readonly BrowserTokenStore _tokenStore;
    private readonly JwtAuthenticationStateProvider _authStateProvider;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public QuizApiClient(HttpClient httpClient, BrowserTokenStore tokenStore, JwtAuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _tokenStore = tokenStore;
        _authStateProvider = authStateProvider;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
        var payload = await ReadPayloadAsync<AuthResponse>(response);

        await _tokenStore.SetTokensAsync(payload.AccessToken, payload.RefreshToken);
        await _authStateProvider.MarkUserAsAuthenticatedAsync(payload.AccessToken);

        return payload;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
        var payload = await ReadPayloadAsync<AuthResponse>(response);

        await _tokenStore.SetTokensAsync(payload.AccessToken, payload.RefreshToken);
        await _authStateProvider.MarkUserAsAuthenticatedAsync(payload.AccessToken);

        return payload;
    }

    public async Task LogoutAsync()
    {
        await AttachAccessTokenAsync();
        var refreshToken = await _tokenStore.GetRefreshTokenAsync() ?? string.Empty;
        await _httpClient.PostAsJsonAsync("api/auth/logout", new LogoutRequest { RefreshToken = refreshToken });
        await _tokenStore.ClearAsync();
    }

    public async Task<IReadOnlyCollection<UserDto>> GetUsersAsync()
    {
        await AttachAccessTokenAsync();
        return await _httpClient.GetFromJsonAsync<IReadOnlyCollection<UserDto>>("api/admin/users", JsonOptions) ?? Array.Empty<UserDto>();
    }

    public async Task<IReadOnlyCollection<RoleDto>> GetRolesAsync()
    {
        await AttachAccessTokenAsync();
        return await _httpClient.GetFromJsonAsync<IReadOnlyCollection<RoleDto>>("api/admin/users/roles", JsonOptions) ?? Array.Empty<RoleDto>();
    }

    public async Task CreateUserAsync(CreateUserRequest request)
    {
        await AttachAccessTokenAsync();
        var response = await _httpClient.PostAsJsonAsync("api/admin/users", request);
        await EnsureSuccessAsync(response);
    }

    public async Task AssignRoleAsync(AssignRoleRequest request)
    {
        await AttachAccessTokenAsync();
        var response = await _httpClient.PostAsJsonAsync("api/admin/users/assign-role", request);
        await EnsureSuccessAsync(response);
    }

    public async Task LockUserAsync(Guid userId, bool lockUser)
    {
        await AttachAccessTokenAsync();
        var response = await _httpClient.PutAsJsonAsync($"api/admin/users/{userId}/lock", new UpdateUserStatusRequest { LockUser = lockUser });
        await EnsureSuccessAsync(response);
    }

    public async Task<PagedResult<QuestionDto>> GetQuestionsAsync(int page = 1, int pageSize = 50)
    {
        await AttachAccessTokenAsync();
        return await _httpClient.GetFromJsonAsync<PagedResult<QuestionDto>>($"api/questions?page={page}&pageSize={pageSize}", JsonOptions)
            ?? new PagedResult<QuestionDto>();
    }

    public async Task<QuestionDto> CreateQuestionAsync(QuestionCreateUpdateRequest request)
    {
        await AttachAccessTokenAsync();
        var response = await _httpClient.PostAsJsonAsync("api/questions", request);
        return await ReadPayloadAsync<QuestionDto>(response);
    }

    public async Task<IReadOnlyCollection<QuizDto>> GetInstructorQuizzesAsync()
    {
        await AttachAccessTokenAsync();
        return await _httpClient.GetFromJsonAsync<IReadOnlyCollection<QuizDto>>("api/quizzes/mine", JsonOptions) ?? Array.Empty<QuizDto>();
    }

    public async Task<IReadOnlyCollection<QuizDto>> GetAllQuizzesAsync()
    {
        await AttachAccessTokenAsync();
        return await _httpClient.GetFromJsonAsync<IReadOnlyCollection<QuizDto>>("api/quizzes/all", JsonOptions) ?? Array.Empty<QuizDto>();
    }

    public async Task<IReadOnlyCollection<QuizDto>> GetAssignedQuizzesAsync()
    {
        await AttachAccessTokenAsync();
        return await _httpClient.GetFromJsonAsync<IReadOnlyCollection<QuizDto>>("api/quizzes/assigned", JsonOptions) ?? Array.Empty<QuizDto>();
    }

    public async Task<QuizDto> CreateQuizAsync(QuizCreateUpdateRequest request)
    {
        await AttachAccessTokenAsync();
        var response = await _httpClient.PostAsJsonAsync("api/quizzes", request);
        return await ReadPayloadAsync<QuizDto>(response);
    }

    public async Task PublishQuizAsync(Guid quizId)
    {
        await AttachAccessTokenAsync();
        var response = await _httpClient.PostAsync($"api/quizzes/{quizId}/publish", null);
        await EnsureSuccessAsync(response);
    }

    public async Task AssignQuizAsync(AssignQuizRequest request)
    {
        await AttachAccessTokenAsync();
        var response = await _httpClient.PostAsJsonAsync("api/quizzes/assign", request);
        await EnsureSuccessAsync(response);
    }

    public async Task<GroupClassDto> CreateGroupAsync(GroupClassCreateRequest request)
    {
        await AttachAccessTokenAsync();
        var response = await _httpClient.PostAsJsonAsync("api/quizzes/groups", request);
        return await ReadPayloadAsync<GroupClassDto>(response);
    }

    public async Task<IReadOnlyCollection<GroupClassDto>> GetGroupsAsync()
    {
        await AttachAccessTokenAsync();
        return await _httpClient.GetFromJsonAsync<IReadOnlyCollection<GroupClassDto>>("api/quizzes/groups", JsonOptions) ?? Array.Empty<GroupClassDto>();
    }

    public async Task<AttemptSessionDto> StartAttemptAsync(Guid quizId)
    {
        await AttachAccessTokenAsync();
        var response = await _httpClient.PostAsJsonAsync("api/attempts/start", new StartAttemptRequest { QuizId = quizId });
        return await ReadPayloadAsync<AttemptSessionDto>(response);
    }

    public async Task<AttemptSessionDto?> GetAttemptSessionAsync(Guid attemptId)
    {
        await AttachAccessTokenAsync();
        var response = await _httpClient.GetAsync($"api/attempts/{attemptId}/session");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return await ReadPayloadAsync<AttemptSessionDto>(response);
    }

    public async Task SaveAnswerAsync(Guid attemptId, SaveAnswerRequest request)
    {
        await AttachAccessTokenAsync();
        var response = await _httpClient.PostAsJsonAsync($"api/attempts/{attemptId}/answers", request);
        await EnsureSuccessAsync(response);
    }

    public async Task<AttemptResultDto> SubmitAttemptAsync(Guid attemptId, bool forceSubmit = false)
    {
        await AttachAccessTokenAsync();
        var response = await _httpClient.PostAsJsonAsync($"api/attempts/{attemptId}/submit", new SubmitAttemptRequest { ForceSubmit = forceSubmit });
        return await ReadPayloadAsync<AttemptResultDto>(response);
    }

    public async Task<IReadOnlyCollection<AttemptResultDto>> GetMyAttemptsAsync()
    {
        await AttachAccessTokenAsync();
        return await _httpClient.GetFromJsonAsync<IReadOnlyCollection<AttemptResultDto>>("api/attempts/mine", JsonOptions) ?? Array.Empty<AttemptResultDto>();
    }

    public async Task<IReadOnlyCollection<AttemptResultDto>> GetAllAttemptsAsync()
    {
        await AttachAccessTokenAsync();
        return await _httpClient.GetFromJsonAsync<IReadOnlyCollection<AttemptResultDto>>("api/attempts/all", JsonOptions) ?? Array.Empty<AttemptResultDto>();
    }

    public async Task<IReadOnlyCollection<AttemptResultDto>> GetAttemptsByQuizAsync(Guid quizId)
    {
        await AttachAccessTokenAsync();
        return await _httpClient.GetFromJsonAsync<IReadOnlyCollection<AttemptResultDto>>($"api/attempts/quiz/{quizId}", JsonOptions) ?? Array.Empty<AttemptResultDto>();
    }

    public async Task<StudentDashboardDto> GetStudentDashboardAsync()
    {
        await AttachAccessTokenAsync();
        return await _httpClient.GetFromJsonAsync<StudentDashboardDto>("api/results/student/dashboard", JsonOptions)
            ?? new StudentDashboardDto();
    }

    public async Task<InstructorQuizAnalyticsDto> GetQuizAnalyticsAsync(Guid quizId)
    {
        await AttachAccessTokenAsync();
        return await _httpClient.GetFromJsonAsync<InstructorQuizAnalyticsDto>($"api/results/quiz/{quizId}/analytics", JsonOptions)
            ?? new InstructorQuizAnalyticsDto();
    }

    public async Task<AdminOverviewDto> GetAdminOverviewAsync()
    {
        await AttachAccessTokenAsync();
        return await _httpClient.GetFromJsonAsync<AdminOverviewDto>("api/results/admin/overview", JsonOptions)
            ?? new AdminOverviewDto();
    }

    public async Task<AttemptResultDto> GradeShortAnswerAsync(Guid attemptId, ManualGradeRequest request)
    {
        await AttachAccessTokenAsync();
        var response = await _httpClient.PostAsJsonAsync($"api/attempts/{attemptId}/grade-short-answer", request);
        return await ReadPayloadAsync<AttemptResultDto>(response);
    }

    private async Task AttachAccessTokenAsync()
    {
        var token = await _tokenStore.GetAccessTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var error = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
            ? $"Request failed with status {(int)response.StatusCode}."
            : error);
    }

    private static async Task<T> ReadPayloadAsync<T>(HttpResponseMessage response)
    {
        await EnsureSuccessAsync(response);

        var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        if (payload is null)
        {
            throw new InvalidOperationException("API response payload is empty.");
        }

        return payload;
    }
}
