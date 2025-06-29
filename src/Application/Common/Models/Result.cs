namespace ChatbotApi.Application.Common.Models;

/// <summary>
/// ผลลัพธ์
/// </summary>
public class Result
{
    internal Result(bool succeeded, IEnumerable<string> errors)
    {
        Succeeded = succeeded;
        Errors = errors.ToArray();
    }

    /// <summary>
    /// สำเร็จหรือไม่
    /// </summary>
    public bool Succeeded { get; init; }

    /// <summary>
    /// ข้อผิดพลาด
    /// </summary>
    public string[] Errors { get; init; }

    /// <summary>
    /// สร้างผลลัพธ์สำเร็จ
    /// </summary>
    /// <returns></returns>
    public static Result Success()
    {
        return new Result(true, Array.Empty<string>());
    }

    /// <summary>
    /// สร้างผลลัพธ์ไม่สำเร็จ
    /// </summary>
    /// <param name="errors"></param>
    /// <returns></returns>
    public static Result Failure(IEnumerable<string> errors)
    {
        return new Result(false, errors);
    }
}
