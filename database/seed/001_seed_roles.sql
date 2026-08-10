-- ============================================================================
-- [DB-SEED-ROLES] Du lieu mau day du TAT CA role trong he thong SEAL
-- Chay SAU khi da ap dung database/migrations/001_seal_schema.sql tren 1 DB
-- rong. Idempotent theo Email (ON CONFLICT DO UPDATE) nen chay lai nhieu lan
-- khong bi loi trung du lieu.
--
-- Cac role duoc tao (mat khau CHUNG cho tat ca: 123456):
--   1. Admin             -> admin@seal.com            (User.IsAdmin = true)
--   2. EventCoordinator  -> ec@seal.com                (EventRole EC cap Event)
--   3. Judge             -> judge@seal.com             (EventRole Judge cap Track)
--   4. Mentor            -> mentor@seal.com            (EventRole Mentor cap Event)
--   5. TeamLeader        -> leader@seal.com            (EventRole TeamLeader cap Team)
--   6. TeamMember        -> member@seal.com            (EventRole TeamMember cap Team)
--   7. Student (chua co role) -> student@seal.com      (User.IsStudent = true, chua vao doi)
--
-- Kem theo 1 School, 1 Event, 1 Round, 1 Track (co Template+Criteria) va
-- 1 Team de cac EventRole o tren co du FK hop le, dung thu duoc ngay cac
-- luong: quan ly doi (Flow 3), nop bai, cham diem.
-- ============================================================================

DO $$
DECLARE
  v_pw text := encode(digest('123456' || 'NDSDIo213n21JDKJSn21m3JDAk24Mfls154', 'sha512'), 'hex');
  v_now timestamptz := now();

  v_school_id text;
  v_event_id text;
  v_round_id text;
  v_track_id text;
  v_team_id text;
  v_template_id text;
  v_criteria_id text;

  v_admin_id text;
  v_ec_id text;
  v_judge_id text;
  v_mentor_id text;
  v_leader_id text;
  v_member_id text;
  v_student_id text;
BEGIN
  -- 1) School dung chung
  SELECT "Id" INTO v_school_id FROM "Schools" WHERE "SchoolName" = 'FPT University' LIMIT 1;
  IF v_school_id IS NULL THEN
    v_school_id := replace(gen_random_uuid()::text, '-', '');
    INSERT INTO "Schools" ("Id", "SchoolName", "CreatedTime", "LastUpdatedTime")
    VALUES (v_school_id, 'FPT University', v_now, v_now);
  END IF;

  -- 2) Bo tieu chi toi thieu (Template + 1 Criteria) de Track co the cham diem
  v_template_id := replace(gen_random_uuid()::text, '-', '');
  INSERT INTO "Templates" ("Id", "TemplateName", "Description", "CreatedTime", "LastUpdatedTime")
  VALUES (v_template_id, 'Bo tieu chi mau (seed)', 'Tao boi database/seed/001_seed_roles.sql', v_now, v_now);

  v_criteria_id := replace(gen_random_uuid()::text, '-', '');
  INSERT INTO "Criterias" ("Id", "CriteriaName", "Description", "IsActive", "CreatedTime", "LastUpdatedTime")
  VALUES (v_criteria_id, 'Chat luong san pham', 'Tieu chi mau', true, v_now, v_now);

  INSERT INTO "TemplateCriterias" ("TemplateId", "CriteriaId", "Weight", "MaxScore")
  VALUES (v_template_id, v_criteria_id, 100.00, 10.00);

  -- 3) 1 Event + 1 Round + 1 Track lam moi truong test cho moi role
  v_event_id := replace(gen_random_uuid()::text, '-', '');
  INSERT INTO "Events" ("Id", "EventName", "Season", "Year", "StartDate", "EndDate",
    "RegistrationStartDate", "RegistrationEndDate", "Description", "Status", "MaxTeams",
    "CreatedTime", "LastUpdatedTime")
  VALUES (v_event_id, '[SEED] Su kien mau day du role', 'SU26', extract(year from v_now)::int,
    v_now - interval '5 days', v_now + interval '30 days',
    v_now - interval '10 days', v_now - interval '6 days',
    'Tao boi database/seed/001_seed_roles.sql de co san 1 Event/Round/Track/Team cho moi role test.',
    true, 10, v_now, v_now);

  v_round_id := replace(gen_random_uuid()::text, '-', '');
  INSERT INTO "Rounds" ("Id", "EventId", "RoundName", "RoundNumber", "StartDate", "EndDate",
    "AdvancementRule", "ScoringStartDate", "ScoringEndDate", "CreatedTime", "LastUpdatedTime")
  VALUES (v_round_id, v_event_id, 'Vong 1', 1, v_now - interval '4 days', v_now + interval '20 days',
    'top:1', v_now + interval '1 days', v_now + interval '25 days', v_now, v_now);

  v_track_id := replace(gen_random_uuid()::text, '-', '');
  INSERT INTO "Tracks" ("Id", "RoundId", "TemplateId", "TrackName", "Description",
    "SubmissionRuleDescription", "StartDate", "EndDate", "ScoringStartDate", "ScoringEndDate",
    "CreatedTime", "LastUpdatedTime")
  VALUES (v_track_id, v_round_id, v_template_id, 'Hang muc mau', 'Tao boi seed script',
    'Link GitHub Repo / Demo / Slide', v_now - interval '4 days', v_now + interval '15 days',
    v_now + interval '15 days', v_now + interval '20 days', v_now, v_now);

  -- 4) 1 Team de gan TeamLeader/TeamMember
  v_team_id := replace(gen_random_uuid()::text, '-', '');
  INSERT INTO "Teams" ("Id", "EventId", "Name", "IsActive", "Status", "CreatedTime", "LastUpdatedTime")
  VALUES (v_team_id, v_event_id, 'Doi mau (seed)', true, 1, v_now, v_now);

  -- 5) Users cho tung role — mat khau chung: 123456
  INSERT INTO "Users" ("Id", "Email", "PasswordHash", "FullName", "IsStudent", "IsAdmin",
    "IsApproved", "IsEmailVerified", "IsFpt", "IsTemporary", "SchoolId", "CreatedTime", "LastUpdatedTime")
  VALUES (replace(gen_random_uuid()::text, '-', ''), 'admin@seal.com', v_pw, 'Seed Admin',
    false, true, true, true, true, false, v_school_id, v_now, v_now)
  ON CONFLICT ("Email") DO UPDATE SET "IsEmailVerified" = true, "IsApproved" = true
  RETURNING "Id" INTO v_admin_id;

  INSERT INTO "Users" ("Id", "Email", "PasswordHash", "FullName", "IsStudent", "IsAdmin",
    "IsApproved", "IsEmailVerified", "IsFpt", "IsTemporary", "SchoolId", "CreatedTime", "LastUpdatedTime")
  VALUES (replace(gen_random_uuid()::text, '-', ''), 'ec@seal.com', v_pw, 'Seed EventCoordinator',
    false, false, true, true, true, false, v_school_id, v_now, v_now)
  ON CONFLICT ("Email") DO UPDATE SET "IsEmailVerified" = true, "IsApproved" = true
  RETURNING "Id" INTO v_ec_id;

  INSERT INTO "Users" ("Id", "Email", "PasswordHash", "FullName", "IsStudent", "IsAdmin",
    "IsApproved", "IsEmailVerified", "IsFpt", "IsTemporary", "SchoolId", "CreatedTime", "LastUpdatedTime")
  VALUES (replace(gen_random_uuid()::text, '-', ''), 'judge@seal.com', v_pw, 'Seed Judge',
    false, false, true, true, true, false, v_school_id, v_now, v_now)
  ON CONFLICT ("Email") DO UPDATE SET "IsEmailVerified" = true, "IsApproved" = true
  RETURNING "Id" INTO v_judge_id;

  INSERT INTO "Users" ("Id", "Email", "PasswordHash", "FullName", "IsStudent", "IsAdmin",
    "IsApproved", "IsEmailVerified", "IsFpt", "IsTemporary", "SchoolId", "CreatedTime", "LastUpdatedTime")
  VALUES (replace(gen_random_uuid()::text, '-', ''), 'mentor@seal.com', v_pw, 'Seed Mentor',
    false, false, true, true, true, false, v_school_id, v_now, v_now)
  ON CONFLICT ("Email") DO UPDATE SET "IsEmailVerified" = true, "IsApproved" = true
  RETURNING "Id" INTO v_mentor_id;

  INSERT INTO "Users" ("Id", "Email", "PasswordHash", "FullName", "IsStudent", "IsAdmin",
    "IsApproved", "IsEmailVerified", "IsFpt", "IsTemporary", "SchoolId", "StudentCode",
    "CreatedTime", "LastUpdatedTime")
  VALUES (replace(gen_random_uuid()::text, '-', ''), 'leader@seal.com', v_pw, 'Seed TeamLeader',
    true, false, true, true, true, false, v_school_id, 'SE000001', v_now, v_now)
  ON CONFLICT ("Email") DO UPDATE SET "IsEmailVerified" = true, "IsApproved" = true
  RETURNING "Id" INTO v_leader_id;

  INSERT INTO "Users" ("Id", "Email", "PasswordHash", "FullName", "IsStudent", "IsAdmin",
    "IsApproved", "IsEmailVerified", "IsFpt", "IsTemporary", "SchoolId", "StudentCode",
    "CreatedTime", "LastUpdatedTime")
  VALUES (replace(gen_random_uuid()::text, '-', ''), 'member@seal.com', v_pw, 'Seed TeamMember',
    true, false, true, true, true, false, v_school_id, 'SE000002', v_now, v_now)
  ON CONFLICT ("Email") DO UPDATE SET "IsEmailVerified" = true, "IsApproved" = true
  RETURNING "Id" INTO v_member_id;

  INSERT INTO "Users" ("Id", "Email", "PasswordHash", "FullName", "IsStudent", "IsAdmin",
    "IsApproved", "IsEmailVerified", "IsFpt", "IsTemporary", "SchoolId", "StudentCode",
    "CreatedTime", "LastUpdatedTime")
  VALUES (replace(gen_random_uuid()::text, '-', ''), 'student@seal.com', v_pw, 'Seed Student (chua co doi)',
    true, false, true, true, true, false, v_school_id, 'SE000003', v_now, v_now)
  ON CONFLICT ("Email") DO UPDATE SET "IsEmailVerified" = true, "IsApproved" = true
  RETURNING "Id" INTO v_student_id;

  -- 6) EventRole cho tung role (Admin khong can EventRole — quyen toan cuc qua IsAdmin)
  INSERT INTO "EventRoles" ("Id", "UserId", "EventId", "TeamId", "TrackId", "RoleName",
    "AssignedAt", "ExpiredAt", "CreatedTime", "LastUpdatedTime")
  VALUES
    (replace(gen_random_uuid()::text, '-', ''), v_ec_id, v_event_id, NULL, NULL,
      'EventCoordinator', v_now, v_now + interval '60 days', v_now, v_now),
    (replace(gen_random_uuid()::text, '-', ''), v_judge_id, v_event_id, NULL, v_track_id,
      'Judge', v_now, v_now + interval '60 days', v_now, v_now),
    (replace(gen_random_uuid()::text, '-', ''), v_mentor_id, v_event_id, NULL, NULL,
      'Mentor', v_now, v_now + interval '60 days', v_now, v_now),
    (replace(gen_random_uuid()::text, '-', ''), v_leader_id, v_event_id, v_team_id, NULL,
      'TeamLeader', v_now, v_now + interval '60 days', v_now, v_now),
    (replace(gen_random_uuid()::text, '-', ''), v_member_id, v_event_id, v_team_id, NULL,
      'TeamMember', v_now, v_now + interval '60 days', v_now, v_now);

  RAISE NOTICE 'Seed du role hoan tat. EventId=%, TeamId=%, TrackId=%', v_event_id, v_team_id, v_track_id;
END $$;
