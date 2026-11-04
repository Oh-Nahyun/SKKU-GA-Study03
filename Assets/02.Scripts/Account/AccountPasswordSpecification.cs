using System.Text.RegularExpressions;

public class AccountPasswordSpecification
{
    // 비밀번호: 전방 탐색 (?=...)으로 대문자·소문자·숫자·기호 각각 포함 여부 검사,
    // 전체는 출력 가능 ASCII('!'~'~' 범위, 공백·한글 제외) 8~30 글자
    // 기호 범위: !-/  :-@  [-`  {-~  (영문·숫자를 뺀 ASCII 기호)
    private static readonly Regex PasswordRegex =
        new Regex(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[!-/:-@\[-`{-~])[!-~]{8,30}$");

    public string ErrorMessage => "비밀번호는 대문자/소문자/숫자/기호 형식이어야 합니다.";

    public bool IsSatisfiedBy(string password)
    {
        return PasswordRegex.IsMatch(password);
    }
}