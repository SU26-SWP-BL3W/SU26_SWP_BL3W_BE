import { Navbar } from "@/components/Navbar";
import { UsersList } from "@/features/user/components/UsersList";

export default function UsersPage() {
  return (
    <>
      <Navbar />
      <UsersList />
    </>
  );
}
