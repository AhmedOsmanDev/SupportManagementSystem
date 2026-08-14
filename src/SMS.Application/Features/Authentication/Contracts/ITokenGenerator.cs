namespace SMS.Application;

public interface ITokenGenerator
{
    TokenResult Create(TokenSubject subject);
}
