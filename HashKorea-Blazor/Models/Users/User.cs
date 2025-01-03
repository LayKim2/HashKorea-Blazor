﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HashKorea.Models;

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    //[Required]
    //public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(5)]
    public string Gender { get; set; } = string.Empty; // 성별

    [StringLength(50)]
    public string Country { get; set; } = string.Empty; // 국가

    [StringLength(255)]
    public string StoragePath { get; set; } = string.Empty; // 실제 저장 경로 (예: "actors/12345.jpg")
    public string PublicUrl { get; set; } = string.Empty;   // 공개 접근 가능한 URL

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; } = false;

    public virtual UserAuth UserAuth { get; set; } = new UserAuth();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new HashSet<UserRole>();
    public virtual ICollection<Term> TermsAgreements { get; set; } = new HashSet<Term>();
    public virtual ICollection<UserPost> UserPosts { get; set; } = new HashSet<UserPost>();
}