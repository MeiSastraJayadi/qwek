using System.Security.Cryptography;
using Qwek.Applications.Ports;

namespace Qwek.Infrastructures.Services;

public class InviteCodeGenerator : IInviteCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 8;

    public string Generate() =>
        RandomNumberGenerator.GetString(Alphabet, CodeLength);
}
