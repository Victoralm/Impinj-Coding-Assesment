namespace PreScreen_API.Models;

public class ResultDto<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ResultDto<T> Failure(IEnumerable<string> errors) =>
        new ResultDto<T> { Success = false, Errors = errors.ToList() };

    public static ResultDto<T> Ok(T data) =>
        new ResultDto<T> { Success = true, Data = data };
}
