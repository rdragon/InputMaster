namespace InputMaster
{
  internal abstract class Actor
  {
    protected Actor()
    {
      Env.CommandCollection.AddActor(this);
    }
  }
}
