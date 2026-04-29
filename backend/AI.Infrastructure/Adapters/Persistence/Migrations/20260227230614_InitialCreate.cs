using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AI.Infrastructure.Adapters.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "history");

            migrationBuilder.EnsureSchema(
                name: "document");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "reports");

            migrationBuilder.EnsureSchema(
                name: "memory");

            migrationBuilder.CreateTable(
                name: "conversations",
                schema: "history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    connection_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_message_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    message_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_categories",
                schema: "document",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "feedback_analysis_reports",
                schema: "history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    analyzed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_feedbacks_analyzed = table.Column<int>(type: "integer", nullable: false),
                    overall_summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    categories_json = table.Column<string>(type: "jsonb", nullable: false),
                    suggestions_json = table.Column<string>(type: "jsonb", nullable: false),
                    high_priority_count = table.Column<int>(type: "integer", nullable: false),
                    medium_priority_count = table.Column<int>(type: "integer", nullable: false),
                    low_priority_count = table.Column<int>(type: "integer", nullable: false),
                    period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feedback_analysis_reports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    active_directory_group = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scheduled_reports",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    original_prompt = table.Column<string>(type: "text", nullable: false),
                    sql_query = table.Column<string>(type: "text", nullable: false),
                    original_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    original_conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cron_expression = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    report_service_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    report_database_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    report_database_service_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    run_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_run_success = table.Column<bool>(type: "boolean", nullable: true),
                    last_error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    notification_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    teams_webhook_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_reports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_memories",
                schema: "memory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    context = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    confidence = table.Column<float>(type: "real", nullable: false, defaultValueSql: "1.0"),
                    usage_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_accessed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_memories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    password_salt = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    authentication_source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ad_username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ad_domain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    active_directory_sid = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    active_directory_dn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_login_attempts = table.Column<int>(type: "integer", nullable: false),
                    lockout_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                schema: "history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    token_count = table.Column<int>(type: "integer", nullable: true),
                    message_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_messages_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalSchema: "history",
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_display_info",
                schema: "document",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    document_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    keywords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    category_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_display_info", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_display_info_document_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "document",
                        principalTable: "document_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "prompt_improvements",
                schema: "history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    analysis_report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    issue = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    suggestion = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    prompt_modification = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    review_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    reviewed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FeedbackAnalysisReportId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_improvements", x => x.id);
                    table.ForeignKey(
                        name: "FK_prompt_improvements_feedback_analysis_reports_FeedbackAnaly~",
                        column: x => x.FeedbackAnalysisReportId,
                        principalSchema: "history",
                        principalTable: "feedback_analysis_reports",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_prompt_improvements_feedback_analysis_reports_analysis_repo~",
                        column: x => x.analysis_report_id,
                        principalSchema: "history",
                        principalTable: "feedback_analysis_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scheduled_report_logs",
                schema: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    error_details = table.Column<string>(type: "text", nullable: true),
                    output_file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    output_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    record_count = table.Column<int>(type: "integer", nullable: true),
                    email_sent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    teams_sent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_report_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_scheduled_report_logs_scheduled_reports_scheduled_report_id",
                        column: x => x.scheduled_report_id,
                        principalSchema: "reports",
                        principalTable: "scheduled_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<string>(type: "character varying(50)", nullable: false),
                    token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    jwt_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_id = table.Column<string>(type: "text", nullable: true),
                    created_by_ip = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    revoked_by_ip = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "identity",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    role_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    assigned_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "identity",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "message_feedbacks",
                schema: "history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_analyzed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    analyzed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MessageContent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message_feedbacks", x => x.id);
                    table.ForeignKey(
                        name: "FK_message_feedbacks_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalSchema: "history",
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_message_feedbacks_messages_message_id",
                        column: x => x.message_id,
                        principalSchema: "history",
                        principalTable: "messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "roles",
                columns: new[] { "id", "active_directory_group", "created_at", "description", "is_system", "name" },
                values: new object[,]
                {
                    { "11111111-1111-1111-1111-111111111111", null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sistem yöneticisi", true, "Admin" },
                    { "22222222-2222-2222-2222-222222222222", null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Standart kullanıcı", true, "User" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_conversations_archived",
                schema: "history",
                table: "conversations",
                column: "is_archived",
                filter: "is_archived = false");

            migrationBuilder.CreateIndex(
                name: "ix_conversations_connection_id",
                schema: "history",
                table: "conversations",
                column: "connection_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_conversations_updated_at",
                schema: "history",
                table: "conversations",
                column: "updated_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_conversations_user_id",
                schema: "history",
                table: "conversations",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversations_user_updated",
                schema: "history",
                table: "conversations",
                columns: new[] { "user_id", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_document_categories_user_id",
                schema: "document",
                table: "document_categories",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_display_info_category_id",
                schema: "document",
                table: "document_display_info",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_document_display_info_file_name",
                schema: "document",
                table: "document_display_info",
                column: "file_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_document_display_info_user_id",
                schema: "document",
                table: "document_display_info",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_feedback_analysis_reports_analyzed_at",
                schema: "history",
                table: "feedback_analysis_reports",
                column: "analyzed_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_message_feedbacks_conversation_id",
                schema: "history",
                table: "message_feedbacks",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_feedbacks_created_at",
                schema: "history",
                table: "message_feedbacks",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_message_feedbacks_message_id",
                schema: "history",
                table: "message_feedbacks",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_feedbacks_message_user_unique",
                schema: "history",
                table: "message_feedbacks",
                columns: new[] { "message_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_message_feedbacks_type_analyzed",
                schema: "history",
                table: "message_feedbacks",
                columns: new[] { "type", "is_analyzed" });

            migrationBuilder.CreateIndex(
                name: "ix_message_feedbacks_user_id",
                schema: "history",
                table: "message_feedbacks",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_messages_content_fts",
                schema: "history",
                table: "messages",
                column: "content")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:TsVectorConfig", "english");

            migrationBuilder.CreateIndex(
                name: "ix_messages_conversation_created_at",
                schema: "history",
                table: "messages",
                columns: new[] { "conversation_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_messages_conversation_id",
                schema: "history",
                table: "messages",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_messages_created_at",
                schema: "history",
                table: "messages",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_prompt_improvements_analysis_report_id",
                schema: "history",
                table: "prompt_improvements",
                column: "analysis_report_id");

            migrationBuilder.CreateIndex(
                name: "ix_prompt_improvements_created_at",
                schema: "history",
                table: "prompt_improvements",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_prompt_improvements_FeedbackAnalysisReportId",
                schema: "history",
                table: "prompt_improvements",
                column: "FeedbackAnalysisReportId");

            migrationBuilder.CreateIndex(
                name: "ix_prompt_improvements_priority",
                schema: "history",
                table: "prompt_improvements",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "ix_prompt_improvements_status",
                schema: "history",
                table: "prompt_improvements",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_expires_at",
                schema: "identity",
                table: "refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_jwt_id",
                schema: "identity",
                table: "refresh_tokens",
                column: "jwt_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token",
                schema: "identity",
                table: "refresh_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_active",
                schema: "identity",
                table: "refresh_tokens",
                columns: new[] { "user_id", "is_revoked", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                schema: "identity",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_ad_group",
                schema: "identity",
                table: "roles",
                column: "active_directory_group");

            migrationBuilder.CreateIndex(
                name: "ix_roles_name",
                schema: "identity",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_report_logs_is_success",
                schema: "reports",
                table: "scheduled_report_logs",
                column: "is_success");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_report_logs_report_id_started_at",
                schema: "reports",
                table: "scheduled_report_logs",
                columns: new[] { "scheduled_report_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_report_logs_scheduled_report_id",
                schema: "reports",
                table: "scheduled_report_logs",
                column: "scheduled_report_id");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_report_logs_started_at",
                schema: "reports",
                table: "scheduled_report_logs",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_reports_is_active",
                schema: "reports",
                table: "scheduled_reports",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_reports_next_run_at",
                schema: "reports",
                table: "scheduled_reports",
                column: "next_run_at");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_reports_user_id",
                schema: "reports",
                table: "scheduled_reports",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_reports_user_id_is_active",
                schema: "reports",
                table: "scheduled_reports",
                columns: new[] { "user_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_user_memories_is_deleted",
                schema: "memory",
                table: "user_memories",
                column: "is_deleted",
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_user_memories_user_id",
                schema: "memory",
                table: "user_memories",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_memories_user_id_category",
                schema: "memory",
                table: "user_memories",
                columns: new[] { "user_id", "category" });

            migrationBuilder.CreateIndex(
                name: "ix_user_memories_user_id_key",
                schema: "memory",
                table: "user_memories",
                columns: new[] { "user_id", "key" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                schema: "identity",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_ad_sid",
                schema: "identity",
                table: "users",
                column: "active_directory_sid");

            migrationBuilder.CreateIndex(
                name: "ix_users_ad_username_domain",
                schema: "identity",
                table: "users",
                columns: new[] { "ad_username", "ad_domain" });

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                schema: "identity",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_is_active",
                schema: "identity",
                table: "users",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                schema: "identity",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_display_info",
                schema: "document");

            migrationBuilder.DropTable(
                name: "message_feedbacks",
                schema: "history");

            migrationBuilder.DropTable(
                name: "prompt_improvements",
                schema: "history");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "scheduled_report_logs",
                schema: "reports");

            migrationBuilder.DropTable(
                name: "user_memories",
                schema: "memory");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "document_categories",
                schema: "document");

            migrationBuilder.DropTable(
                name: "messages",
                schema: "history");

            migrationBuilder.DropTable(
                name: "feedback_analysis_reports",
                schema: "history");

            migrationBuilder.DropTable(
                name: "scheduled_reports",
                schema: "reports");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "conversations",
                schema: "history");
        }
    }
}
