namespace be_da.Application.DTOs;

public sealed record ProductImage(byte[] Data, string ContentType);

public enum ProductImageResult { Saved, Missing, Invalid, Unavailable }
